import { exportJWK, generateKeyPair, SignJWT } from "jose";
import { env } from "cloudflare:test";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { handleInternalIdentity } from "./internal-identity.js";

const ISSUER = "https://trainfree.cloudflareaccess.com";
const ADMIN_AUDIENCE = "trainfree-admin-audience";
const WORKOUT_AUDIENCE = "trainfree-workout-audience";
const KID = "test-key";
const ADMINISTRATOR_ROLE_ID = "ROL-A3F7K2";

let privateKey;
let publicJwk;

beforeAll(async () => {
    const keyPair = await generateKeyPair("RS256");
    privateKey = keyPair.privateKey;
    publicJwk = await exportJWK(keyPair.publicKey);
    publicJwk.kid = KID;
    publicJwk.alg = "RS256";
    publicJwk.use = "sig";
});

function fakeJwksResponse() {
    return new Response(JSON.stringify({ keys: [publicJwk] }), {
        headers: { "content-type": "application/json", "cache-control": "public, max-age=3600" },
    });
}

function fakeJwksFetcher() {
    return vi.fn().mockResolvedValue(fakeJwksResponse());
}

function signToken({
    issuer = ISSUER,
    audience = ADMIN_AUDIENCE,
    email = "user@example.com",
    expiresInSeconds = 3600,
} = {}) {
    return new SignJWT({ email })
        .setProtectedHeader({ alg: "RS256", kid: KID })
        .setIssuer(issuer)
        .setAudience(audience)
        .setIssuedAt()
        .setExpirationTime(Math.floor(Date.now() / 1000) + expiresInSeconds)
        .sign(privateKey);
}

// No default `caller`/`key` values here -- a test that wants a valid request must
// state so explicitly, so a test that means to omit a header (missing caller, missing
// key) can't accidentally fall back to a passing value.
function requestFor({ token, caller, key } = {}) {
    const headers = {};
    if (token) {
        headers.Cookie = `CF_Authorization=${token}`;
    }
    if (caller !== undefined) {
        headers["X-Trainfree-Caller"] = caller;
    }
    if (key !== undefined) {
        headers["X-Trainfree-Internal-Key"] = key;
    }
    return new Request("https://identity.trainfree.workers.dev/internal/identity", { headers });
}

async function seedLogin(db, { providerName = "cloudflare-access", providerId }) {
    const now = new Date().toISOString();
    const result = await db
        .prepare(
            "INSERT INTO logins (provider_name, provider_id, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
        .bind(providerName, providerId, now, now)
        .run();
    return result.meta.last_row_id;
}

async function seedUser(db, { userId, loginId, roleId }) {
    const now = new Date().toISOString();
    await db
        .prepare(
            "INSERT INTO users (user_id, login_id, role_id, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
        )
        .bind(userId, loginId, roleId, now, now)
        .run();
}

async function seedProvisionedAdministrator(email) {
    const loginId = await seedLogin(env.DB, { providerId: email });
    await seedUser(env.DB, { userId: "USR-ADMIN001", loginId, roleId: ADMINISTRATOR_ROLE_ID });
}

describe("handleInternalIdentity -- internal-key check ordering", () => {
    it("responds 404 without evaluating the JWT when the key is missing", async () => {
        const token = await signToken();
        const fetcher = fakeJwksFetcher();

        const response = await handleInternalIdentity(
            requestFor({ token, caller: "admin" }),
            env,
            { fetcher },
        );

        expect(response.status).toBe(404);
        expect(response.headers.get("content-type")).toContain("application/json");
        expect(await response.json()).toEqual({ error: expect.any(String) });
        expect(fetcher).not.toHaveBeenCalled();
    });

    it("responds 404 without evaluating the JWT when the key is wrong for the named caller", async () => {
        const token = await signToken();
        const fetcher = fakeJwksFetcher();

        const response = await handleInternalIdentity(
            requestFor({ token, caller: "admin", key: "not-the-real-key" }),
            env,
            { fetcher },
        );

        expect(response.status).toBe(404);
        expect(fetcher).not.toHaveBeenCalled();
    });

    it("responds 404 when admin's key is presented for caller workout", async () => {
        const token = await signToken({ audience: WORKOUT_AUDIENCE });
        const fetcher = fakeJwksFetcher();

        const response = await handleInternalIdentity(
            requestFor({ token, caller: "workout", key: env.ADMIN_INTERNAL_KEY }),
            env,
            { fetcher },
        );

        expect(response.status).toBe(404);
        expect(fetcher).not.toHaveBeenCalled();
    });
});

describe("handleInternalIdentity -- response matrix", () => {
    it("responds 200 with email, userId, and role for a valid key and JWT resolving a provisioned identity", async () => {
        await seedProvisionedAdministrator("admin@example.com");
        const token = await signToken({ email: "admin@example.com" });

        const response = await handleInternalIdentity(
            requestFor({ token, caller: "admin", key: env.ADMIN_INTERNAL_KEY }),
            env,
            { fetcher: fakeJwksFetcher() },
        );

        expect(response.status).toBe(200);
        expect(response.headers.get("content-type")).toContain("application/json");
        expect(await response.json()).toEqual({
            email: "admin@example.com",
            userId: "USR-ADMIN001",
            role: "Administrator",
        });
    });

    it("responds 401 when the CF_Authorization cookie is missing", async () => {
        const response = await handleInternalIdentity(
            requestFor({ caller: "admin", key: env.ADMIN_INTERNAL_KEY }),
            env,
            { fetcher: fakeJwksFetcher() },
        );

        expect(response.status).toBe(401);
        expect(response.headers.get("content-type")).toContain("application/json");
        expect(await response.json()).toEqual({ error: expect.any(String) });
    });

    it("responds 401 for an expired JWT", async () => {
        const token = await signToken({ expiresInSeconds: -60 });

        const response = await handleInternalIdentity(
            requestFor({ token, caller: "admin", key: env.ADMIN_INTERNAL_KEY }),
            env,
            { fetcher: fakeJwksFetcher() },
        );

        expect(response.status).toBe(401);
    });

    it("responds 401 for a JWT with the wrong issuer", async () => {
        const token = await signToken({ issuer: "https://not-trainfree.cloudflareaccess.com" });

        const response = await handleInternalIdentity(
            requestFor({ token, caller: "admin", key: env.ADMIN_INTERNAL_KEY }),
            env,
            { fetcher: fakeJwksFetcher() },
        );

        expect(response.status).toBe(401);
    });

    it("responds 401 for a JWT whose audience does not match the named caller's", async () => {
        const token = await signToken({ audience: WORKOUT_AUDIENCE });

        const response = await handleInternalIdentity(
            requestFor({ token, caller: "admin", key: env.ADMIN_INTERNAL_KEY }),
            env,
            { fetcher: fakeJwksFetcher() },
        );

        expect(response.status).toBe(401);
    });

    it("responds 401 when X-Trainfree-Caller is missing", async () => {
        const token = await signToken();

        const response = await handleInternalIdentity(
            requestFor({ token, key: env.ADMIN_INTERNAL_KEY }),
            env,
            { fetcher: fakeJwksFetcher() },
        );

        expect(response.status).toBe(401);
        expect(await response.json()).toEqual({ error: expect.any(String) });
    });

    it("responds 401 when X-Trainfree-Caller names an unknown caller", async () => {
        const token = await signToken();

        const response = await handleInternalIdentity(
            requestFor({ token, caller: "not-a-real-caller", key: env.ADMIN_INTERNAL_KEY }),
            env,
            { fetcher: fakeJwksFetcher() },
        );

        expect(response.status).toBe(401);
    });

    it("responds 403 when the JWT authenticates but no logins/users pair matches", async () => {
        const token = await signToken({ email: "unprovisioned@example.com" });

        const response = await handleInternalIdentity(
            requestFor({ token, caller: "admin", key: env.ADMIN_INTERNAL_KEY }),
            env,
            { fetcher: fakeJwksFetcher() },
        );

        expect(response.status).toBe(403);
        expect(response.headers.get("content-type")).toContain("application/json");
        expect(await response.json()).toEqual({ error: expect.any(String) });
    });
});

describe("handleInternalIdentity -- infrastructure failures", () => {
    it("responds 503 when the JWKS fetch fails with no cached copy available", async () => {
        const token = await signToken();
        const fetcher = vi.fn().mockResolvedValue(new Response("boom", { status: 500 }));

        const response = await handleInternalIdentity(
            requestFor({ token, caller: "admin", key: env.ADMIN_INTERNAL_KEY }),
            env,
            { fetcher },
        );

        expect(response.status).toBe(503);
        expect(response.headers.get("content-type")).toContain("application/json");
        expect(await response.json()).toEqual({ error: expect.any(String) });
    });

    it("responds 503, not an uncaught exception, when the D1 role lookup throws", async () => {
        const token = await signToken();
        const throwingDb = {
            prepare: () => ({
                bind: () => ({
                    first: () => Promise.reject(new Error("D1 query timed out")),
                }),
            }),
        };

        const response = await handleInternalIdentity(
            requestFor({ token, caller: "admin", key: env.ADMIN_INTERNAL_KEY }),
            env,
            { fetcher: fakeJwksFetcher(), db: throwingDb },
        );

        expect(response.status).toBe(503);
        expect(await response.json()).toEqual({ error: expect.any(String) });
    });
});
