import { env } from "cloudflare:test";
import { afterEach, describe, expect, it, vi } from "vitest";
import { checkIdentity, isAdministrator } from "./identity.js";

const LOCAL_DEV_USER_ID = "USR-LOCALDEV";

function makeEnv(overrides = {}) {
    return {
        DB: env.DB,
        IDENTITY: { fetch: vi.fn() },
        ADMIN_INTERNAL_KEY: "the-internal-key",
        ...overrides,
    };
}

function makeRequest({ token, cookie } = {}) {
    const headers = {};
    if (token) {
        headers["Cf-Access-Jwt-Assertion"] = token;
    }
    if (cookie) {
        headers.Cookie = cookie;
    }
    return new Request("http://worker/api/programs", { headers });
}

describe("checkIdentity", () => {
    it("returns ok:true with the identity on a 200 from cloudfare-identity", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            new Response(
                JSON.stringify({ email: "a@x.com", userId: "USR-ABC123", role: "Administrator" }),
                { status: 200 },
            ),
        );

        const result = await checkIdentity(makeRequest({ token: "access-jwt" }), testEnv);

        expect(result).toEqual({
            ok: true,
            identity: { email: "a@x.com", userId: "USR-ABC123", role: "Administrator" },
        });
    });

    it("sends the application, credential and Access token headers to /v1/identity", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(new Response(null, { status: 401 }));

        await checkIdentity(makeRequest({ token: "access-jwt" }), testEnv);

        expect(testEnv.IDENTITY.fetch).toHaveBeenCalledTimes(1);
        const [url, init] = testEnv.IDENTITY.fetch.mock.calls[0];
        expect(new URL(url).pathname).toBe("/v1/identity");
        expect(init.headers).toEqual({
            "X-Identity-Application": "Trainfree.Admin",
            "X-Identity-Credential": "the-internal-key",
            "X-Identity-Token": "access-jwt",
        });
    });

    it("sends the Access-supplied token, not the rewritten CF_Authorization cookie", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(new Response(null, { status: 401 }));

        await checkIdentity(
            makeRequest({ token: "access-jwt", cookie: "CF_Authorization=rewritten" }),
            testEnv,
        );

        const [, init] = testEnv.IDENTITY.fetch.mock.calls[0];
        expect(init.headers["X-Identity-Token"]).toBe("access-jwt");
        expect(init.headers.Cookie).toBeUndefined();
    });

    it("returns ok:false status:401 when cloudfare-identity responds 401", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(new Response(null, { status: 401 }));

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 401 });
    });

    it("returns ok:false status:403 when cloudfare-identity responds 403", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(new Response(null, { status: 403 }));

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 403 });
    });

    it("returns ok:false status:503 when cloudfare-identity responds 503", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(new Response(null, { status: 503 }));

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });

    it("returns ok:false status:503 for an unexpected status, e.g. a 404 from a missing/wrong internal key", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(new Response(null, { status: 404 }));

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });

    it("returns ok:false status:503 when a 200 response body is not valid JSON", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(new Response("not json", { status: 200 }));

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });

    it("returns ok:false status:503 when a 200 response body is missing userId", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            new Response(JSON.stringify({ email: "a@x.com", role: "Administrator" }), {
                status: 200,
            }),
        );

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });

    it("returns ok:false status:503 when a 200 response body has an empty userId", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            new Response(
                JSON.stringify({ email: "a@x.com", userId: "", role: "Administrator" }),
                { status: 200 },
            ),
        );

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });

    it("returns ok:false status:503 when a 200 response body is missing email", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            new Response(JSON.stringify({ userId: "USR-ABC123", role: "Administrator" }), {
                status: 200,
            }),
        );

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });

    it("returns ok:false status:503 when a 200 response body is missing role", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            new Response(JSON.stringify({ email: "a@x.com", userId: "USR-ABC123" }), {
                status: 200,
            }),
        );

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });

    it("returns ok:false status:503 when a 200 response body has a role cloudfare-identity doesn't define", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            new Response(
                JSON.stringify({ email: "a@x.com", userId: "USR-ABC123", role: "Owner" }),
                { status: 200 },
            ),
        );

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });

    it("returns ok:false status:503 when the service-binding call throws", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockRejectedValue(new Error("timed out"));

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });

    it("returns ok:false status:503 in a deployed context missing the IDENTITY binding", async () => {
        const testEnv = makeEnv({ IDENTITY: undefined });

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });

    it("returns ok:false status:503 in a deployed context missing ADMIN_INTERNAL_KEY", async () => {
        const testEnv = makeEnv({ ADMIN_INTERNAL_KEY: undefined });

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });

    it("uses the real cloudfare-identity call, not the local-dev bypass, when LOCAL_DEV_BYPASS is the string \"false\"", async () => {
        const testEnv = makeEnv({ LOCAL_DEV_BYPASS: "false" });
        testEnv.IDENTITY.fetch.mockResolvedValue(
            new Response(
                JSON.stringify({ email: "a@x.com", userId: "USR-ABC123", role: "Administrator" }),
                { status: 200 },
            ),
        );

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({
            ok: true,
            identity: { email: "a@x.com", userId: "USR-ABC123", role: "Administrator" },
        });
        expect(testEnv.IDENTITY.fetch).toHaveBeenCalled();
    });

    it("substitutes the synthetic identity without calling IDENTITY when LOCAL_DEV_BYPASS is set", async () => {
        const testEnv = makeEnv({ LOCAL_DEV_BYPASS: "true" });

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({
            ok: true,
            identity: {
                email: "local-dev@trainfree.local",
                role: "Administrator",
                userId: LOCAL_DEV_USER_ID,
            },
        });
        expect(testEnv.IDENTITY.fetch).not.toHaveBeenCalled();
    });

    it("substitutes the synthetic identity under LOCAL_DEV_BYPASS with no identity rows in the database", async () => {
        const testEnv = makeEnv({ LOCAL_DEV_BYPASS: "true", DB: undefined });

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({
            ok: true,
            identity: {
                email: "local-dev@trainfree.local",
                role: "Administrator",
                userId: LOCAL_DEV_USER_ID,
            },
        });
    });

    it("returns ok:false status:503 when LOCAL_DEV_BYPASS is unset and the IDENTITY binding is absent", async () => {
        const testEnv = makeEnv({ IDENTITY: undefined });

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 503 });
    });
});

describe("checkIdentity sessionId handling", () => {
    afterEach(() => {
        vi.restoreAllMocks();
    });

    const SESSION_ID = "0123456789abcdef0123456789abcdef";

    function identityResponse(body) {
        return new Response(JSON.stringify(body), { status: 200 });
    }

    it("passes through a well-formed sessionId", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            identityResponse({
                email: "a@x.com",
                userId: "USR-ABC123",
                role: "Administrator",
                sessionId: SESSION_ID,
            }),
        );

        const result = await checkIdentity(makeRequest(), testEnv, () => undefined);

        expect(result.ok).toBe(true);
        expect(result.identity.sessionId).toBe(SESSION_ID);
    });

    it.each([
        ["a non-string", 12345],
        ["null", null],
        ["too short", "abc123"],
        ["too long", `${SESSION_ID}0`],
        ["uppercase hex", SESSION_ID.replace("abcdef", "ABCDEF")],
        ["non-hex characters", "g".repeat(32)],
    ])("drops the sessionId and still resolves the identity when sessionId is %s", async (_label, sessionId) => {
        const warn = vi.spyOn(console, "warn").mockImplementation(() => {});
        const span = { setAttributes: vi.fn() };
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            identityResponse({
                email: "a@x.com",
                userId: "USR-ABC123",
                role: "Administrator",
                sessionId,
            }),
        );

        const result = await checkIdentity(makeRequest(), testEnv, () => span);

        expect(result.ok).toBe(true);
        expect(result.identity).toEqual({
            email: "a@x.com",
            userId: "USR-ABC123",
            role: "Administrator",
        });
        expect(span.setAttributes).toHaveBeenCalledWith({
            "user.id": "USR-ABC123",
            "user.role": "Administrator",
        });
        expect(warn).toHaveBeenCalledTimes(1);
    });

    it("sets user.id, user.role and session.id on the span once identity resolves", async () => {
        const span = { setAttributes: vi.fn() };
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            identityResponse({
                email: "a@x.com",
                userId: "USR-ABC123",
                role: "Administrator",
                sessionId: SESSION_ID,
            }),
        );

        await checkIdentity(makeRequest(), testEnv, () => span);

        expect(span.setAttributes).toHaveBeenCalledWith({
            "user.id": "USR-ABC123",
            "user.role": "Administrator",
            "session.id": SESSION_ID,
        });
    });

    it("sets no session.id when cloudfare-identity returns none", async () => {
        const span = { setAttributes: vi.fn() };
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            identityResponse({ email: "a@x.com", userId: "USR-ABC123", role: "User" }),
        );

        await checkIdentity(makeRequest(), testEnv, () => span);

        expect(span.setAttributes).toHaveBeenCalledWith({
            "user.id": "USR-ABC123",
            "user.role": "User",
        });
    });

    it("sets no attributes when the identity does not resolve", async () => {
        const span = { setAttributes: vi.fn() };
        const testEnv = makeEnv({ ADMIN_INTERNAL_KEY: undefined });

        const result = await checkIdentity(makeRequest(), testEnv, () => span);

        expect(result).toEqual({ ok: false, status: 503 });
        expect(span.setAttributes).not.toHaveBeenCalled();
    });

    it("sets user attributes but no session.id under LOCAL_DEV_BYPASS", async () => {
        const span = { setAttributes: vi.fn() };
        const testEnv = makeEnv({ LOCAL_DEV_BYPASS: "true" });

        await checkIdentity(makeRequest(), testEnv, () => span);

        expect(span.setAttributes).toHaveBeenCalledWith({
            "user.id": LOCAL_DEV_USER_ID,
            "user.role": "Administrator",
        });
    });
});

describe("isAdministrator", () => {
    it("returns true for an Administrator identity", () => {
        expect(isAdministrator({ role: "Administrator" })).toBe(true);
    });

    it("returns false for a User identity", () => {
        expect(isAdministrator({ role: "User" })).toBe(false);
    });
});
