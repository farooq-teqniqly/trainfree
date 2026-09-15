import { exportJWK, generateKeyPair, SignJWT } from "jose";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { AudienceNotConfiguredError, JwtVerificationError, verifyAccessJwt } from "./jwt.js";

const ISSUER = "https://trainfree.cloudflareaccess.com";
const AUDIENCE = "trainfree-admin-audience";
const KID = "test-key";

let privateKey;
let getJwks;

beforeAll(async () => {
    const keyPair = await generateKeyPair("RS256");
    privateKey = keyPair.privateKey;
    const publicJwk = await exportJWK(keyPair.publicKey);
    publicJwk.kid = KID;
    publicJwk.alg = "RS256";
    publicJwk.use = "sig";

    getJwks = async () => ({ keys: [publicJwk] });
});

function sign({ issuer = ISSUER, audience = AUDIENCE, expiresInSeconds = 3600 } = {}) {
    return new SignJWT({ email: "user@example.com" })
        .setProtectedHeader({ alg: "RS256", kid: KID })
        .setIssuer(issuer)
        .setAudience(audience)
        .setIssuedAt()
        .setExpirationTime(Math.floor(Date.now() / 1000) + expiresInSeconds)
        .sign(privateKey);
}

describe("verifyAccessJwt", () => {
    it("accepts a valid signature/issuer/audience/expiry", async () => {
        const token = await sign();

        const payload = await verifyAccessJwt(token, {
            getJwks,
            expectedIssuer: ISSUER,
            expectedAudience: AUDIENCE,
        });

        expect(payload.email).toBe("user@example.com");
    });

    it("rejects a token with the wrong issuer", async () => {
        const token = await sign({ issuer: "https://not-trainfree.cloudflareaccess.com" });

        await expect(
            verifyAccessJwt(token, { getJwks, expectedIssuer: ISSUER, expectedAudience: AUDIENCE }),
        ).rejects.toThrow(JwtVerificationError);
    });

    it("rejects an expired token", async () => {
        const token = await sign({ expiresInSeconds: -60 });

        await expect(
            verifyAccessJwt(token, { getJwks, expectedIssuer: ISSUER, expectedAudience: AUDIENCE }),
        ).rejects.toThrow(JwtVerificationError);
    });

    it("accepts a multi-element aud array containing the expected audience", async () => {
        const token = await sign({ audience: [AUDIENCE, "trainfree-workout-audience"] });

        const payload = await verifyAccessJwt(token, {
            getJwks,
            expectedIssuer: ISSUER,
            expectedAudience: AUDIENCE,
        });

        expect(payload.aud).toEqual([AUDIENCE, "trainfree-workout-audience"]);
    });

    it("rejects an aud array that does not contain the expected audience", async () => {
        const token = await sign({ audience: ["trainfree-workout-audience"] });

        await expect(
            verifyAccessJwt(token, { getJwks, expectedIssuer: ISSUER, expectedAudience: AUDIENCE }),
        ).rejects.toThrow(JwtVerificationError);
    });

    it("rejects with AudienceNotConfiguredError when no audience is configured for the caller, without checking any JWT claim", async () => {
        const token = await sign();

        await expect(
            verifyAccessJwt(token, { getJwks, expectedIssuer: ISSUER, expectedAudience: undefined }),
        ).rejects.toThrow(AudienceNotConfiguredError);
    });

    it("rejects a token with no exp claim", async () => {
        const token = await new SignJWT({ email: "user@example.com" })
            .setProtectedHeader({ alg: "RS256", kid: KID })
            .setIssuer(ISSUER)
            .setAudience(AUDIENCE)
            .setIssuedAt()
            .sign(privateKey);

        await expect(
            verifyAccessJwt(token, { getJwks, expectedIssuer: ISSUER, expectedAudience: AUDIENCE }),
        ).rejects.toThrow(JwtVerificationError);
    });

    it("retries once with a forced-refresh JWKS when the cached set lacks the token's kid, then succeeds", async () => {
        const rotatedKeyPair = await generateKeyPair("RS256");
        const rotatedPublicJwk = await exportJWK(rotatedKeyPair.publicKey);
        rotatedPublicJwk.kid = "rotated-key";
        rotatedPublicJwk.alg = "RS256";
        rotatedPublicJwk.use = "sig";

        const token = await new SignJWT({ email: "rotated@example.com" })
            .setProtectedHeader({ alg: "RS256", kid: "rotated-key" })
            .setIssuer(ISSUER)
            .setAudience(AUDIENCE)
            .setIssuedAt()
            .setExpirationTime(Math.floor(Date.now() / 1000) + 3600)
            .sign(rotatedKeyPair.privateKey);

        // Simulates a Worker whose cache still has the pre-rotation key set until a
        // forced refresh is requested.
        const staleThenFreshGetJwks = vi.fn(async (options) =>
            options?.forceRefresh ? { keys: [rotatedPublicJwk] } : (await getJwks()),
        );

        const payload = await verifyAccessJwt(token, {
            getJwks: staleThenFreshGetJwks,
            expectedIssuer: ISSUER,
            expectedAudience: AUDIENCE,
        });

        expect(payload.email).toBe("rotated@example.com");
        expect(staleThenFreshGetJwks).toHaveBeenCalledTimes(2);
    });

    it("throws JwtVerificationError when the kid is still missing after a forced refresh", async () => {
        const token = await new SignJWT({ email: "user@example.com" })
            .setProtectedHeader({ alg: "RS256", kid: "never-published" })
            .setIssuer(ISSUER)
            .setAudience(AUDIENCE)
            .setIssuedAt()
            .setExpirationTime(Math.floor(Date.now() / 1000) + 3600)
            .sign(privateKey);
        const stillStaleGetJwks = vi.fn(async () => getJwks());

        await expect(
            verifyAccessJwt(token, {
                getJwks: stillStaleGetJwks,
                expectedIssuer: ISSUER,
                expectedAudience: AUDIENCE,
            }),
        ).rejects.toThrow(JwtVerificationError);
        expect(stillStaleGetJwks).toHaveBeenCalledTimes(2);
    });

    it("propagates a forced-refresh infrastructure failure unwrapped, not as JwtVerificationError", async () => {
        const token = await new SignJWT({ email: "user@example.com" })
            .setProtectedHeader({ alg: "RS256", kid: "never-published" })
            .setIssuer(ISSUER)
            .setAudience(AUDIENCE)
            .setIssuedAt()
            .setExpirationTime(Math.floor(Date.now() / 1000) + 3600)
            .sign(privateKey);
        const refreshFailure = new Error("JWKS fetch failed with status 500");
        const getJwksWithFailingRefresh = vi.fn(async (options) =>
            options?.forceRefresh ? Promise.reject(refreshFailure) : getJwks(),
        );

        await expect(
            verifyAccessJwt(token, {
                getJwks: getJwksWithFailingRefresh,
                expectedIssuer: ISSUER,
                expectedAudience: AUDIENCE,
            }),
        ).rejects.toBe(refreshFailure);
    });
});
