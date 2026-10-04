import { env } from "cloudflare:test";
import { describe, expect, it, vi } from "vitest";
import { checkIdentity, isAdministrator } from "./identity.js";

// test/apply-migrations.js seeds a local-dev Administrator identity
// (provider_id "local-dev@trainfree.local", userId "USR-LOCALDEV") into every test
// file's D1 instance -- see that file's comment for why.
const SEEDED_LOCAL_DEV_USER_ID = "USR-LOCALDEV";

function makeEnv(overrides = {}) {
    return {
        DB: env.DB,
        IDENTITY: { fetch: vi.fn() },
        ADMIN_INTERNAL_KEY: "the-internal-key",
        ...overrides,
    };
}

function makeRequest(cookie) {
    const headers = cookie ? { Cookie: cookie } : {};
    return new Request("http://worker/api/programs", { headers });
}

describe("checkIdentity", () => {
    it("returns ok:true with the identity on a 200 from IdentityApi", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            new Response(
                JSON.stringify({ email: "a@x.com", userId: "USR-ABC123", role: "Administrator" }),
                { status: 200 },
            ),
        );

        const result = await checkIdentity(makeRequest("CF_Authorization=jwt"), testEnv);

        expect(result).toEqual({
            ok: true,
            identity: { email: "a@x.com", userId: "USR-ABC123", role: "Administrator" },
        });
        expect(testEnv.IDENTITY.fetch).toHaveBeenCalledWith(
            "https://trainfree-identity-api/internal/identity",
            expect.objectContaining({
                headers: expect.objectContaining({
                    "X-Trainfree-Caller": "admin",
                    "X-Trainfree-Internal-Key": "the-internal-key",
                    Cookie: "CF_Authorization=jwt",
                }),
            }),
        );
    });

    it("returns ok:false status:401 when IdentityApi responds 401", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(new Response(null, { status: 401 }));

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 401 });
    });

    it("returns ok:false status:403 when IdentityApi responds 403", async () => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(new Response(null, { status: 403 }));

        const result = await checkIdentity(makeRequest(), testEnv);

        expect(result).toEqual({ ok: false, status: 403 });
    });

    it("returns ok:false status:503 when IdentityApi responds 503", async () => {
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

    it("returns ok:false status:503 when a 200 response body has a role IdentityApi doesn't define", async () => {
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

    it("uses the real IdentityApi call, not the local-dev bypass, when LOCAL_DEV_BYPASS is the string \"false\"", async () => {
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
                userId: SEEDED_LOCAL_DEV_USER_ID,
            },
        });
        expect(testEnv.IDENTITY.fetch).not.toHaveBeenCalled();
    });

    it("throws a clear error when LOCAL_DEV_BYPASS is set but no seed row exists", async () => {
        await env.DB.prepare(
            `DELETE FROM users WHERE login_id IN (
                 SELECT id FROM logins WHERE provider_name = ? AND provider_id = ?
             )`,
        )
            .bind("cloudflare-access", "local-dev@trainfree.local")
            .run();
        await env.DB.prepare("DELETE FROM logins WHERE provider_name = ? AND provider_id = ?")
            .bind("cloudflare-access", "local-dev@trainfree.local")
            .run();
        const testEnv = makeEnv({ LOCAL_DEV_BYPASS: "true" });

        await expect(checkIdentity(makeRequest(), testEnv)).rejects.toThrow(
            /no local-dev identity is seeded/,
        );
    });
});

describe("checkIdentity sessionId handling", () => {
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
        ["too short", "abc123"],
        ["too long", `${SESSION_ID}0`],
        ["uppercase hex", SESSION_ID.replace("abcdef", "ABCDEF")],
        ["non-hex characters", "g".repeat(32)],
    ])("returns ok:false status:503 when sessionId is %s", async (_label, sessionId) => {
        const testEnv = makeEnv();
        testEnv.IDENTITY.fetch.mockResolvedValue(
            identityResponse({
                email: "a@x.com",
                userId: "USR-ABC123",
                role: "Administrator",
                sessionId,
            }),
        );

        const result = await checkIdentity(makeRequest(), testEnv, () => undefined);

        expect(result).toEqual({ ok: false, status: 503 });
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

    it("sets no session.id when IdentityApi returns none", async () => {
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
            "user.id": SEEDED_LOCAL_DEV_USER_ID,
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
