import { exportJWK, generateKeyPair, SignJWT } from "jose";
import { beforeAll, describe, expect, it } from "vitest";
import { JwtVerificationError } from "../jwt.js";
import { extractIdentity } from "./cloudflare-access.js";

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

async function signToken(email = "user@example.com", nonce) {
    const claims = email === null ? {} : { email };
    if (nonce !== undefined) {
        claims.identity_nonce = nonce;
    }
    return new SignJWT(claims)
        .setProtectedHeader({ alg: "RS256", kid: KID })
        .setIssuer(ISSUER)
        .setAudience(AUDIENCE)
        .setIssuedAt()
        .setExpirationTime(Math.floor(Date.now() / 1000) + 3600)
        .sign(privateKey);
}

function requestWithCookie(token) {
    return new Request("https://identity.trainfree.workers.dev/internal/identity", {
        headers: token ? { Cookie: `CF_Authorization=${token}` } : {},
    });
}

describe("extractIdentity", () => {
    it("returns the identity's email for a valid CF_Authorization cookie", async () => {
        const token = await signToken("user@example.com");

        const identity = await extractIdentity(requestWithCookie(token), {
            getJwks,
            expectedIssuer: ISSUER,
            expectedAudience: AUDIENCE,
        });

        expect(identity).toEqual({ email: "user@example.com" });
    });

    it("derives a 32-character lowercase hex sessionId that is not the raw nonce", async () => {
        const token = await signToken("user@example.com", "nonce-one");

        const identity = await extractIdentity(requestWithCookie(token), {
            getJwks,
            expectedIssuer: ISSUER,
            expectedAudience: AUDIENCE,
        });

        expect(identity.sessionId).toMatch(/^[0-9a-f]{32}$/);
        expect(identity.sessionId).not.toContain("nonce-one");
    });

    it("derives the same sessionId for the same nonce and a different one for a different nonce", async () => {
        const options = { getJwks, expectedIssuer: ISSUER, expectedAudience: AUDIENCE };

        const first = await extractIdentity(requestWithCookie(await signToken("a@example.com", "n1")), options);
        const again = await extractIdentity(requestWithCookie(await signToken("b@example.com", "n1")), options);
        const other = await extractIdentity(requestWithCookie(await signToken("a@example.com", "n2")), options);

        expect(again.sessionId).toBe(first.sessionId);
        expect(other.sessionId).not.toBe(first.sessionId);
    });

    it.each([
        ["empty string", ""],
        ["number", 42],
    ])("omits sessionId when identity_nonce is a %s", async (_label, nonce) => {
        const token = await signToken("user@example.com", nonce);

        const identity = await extractIdentity(requestWithCookie(token), {
            getJwks,
            expectedIssuer: ISSUER,
            expectedAudience: AUDIENCE,
        });

        expect(identity).toEqual({ email: "user@example.com" });
    });

    it("throws JwtVerificationError when the CF_Authorization cookie is missing", async () => {
        await expect(
            extractIdentity(requestWithCookie(null), {
                getJwks,
                expectedIssuer: ISSUER,
                expectedAudience: AUDIENCE,
            }),
        ).rejects.toThrow(JwtVerificationError);
    });

    it("throws JwtVerificationError when the token has no email claim", async () => {
        const token = await signToken(null);

        await expect(
            extractIdentity(requestWithCookie(token), {
                getJwks,
                expectedIssuer: ISSUER,
                expectedAudience: AUDIENCE,
            }),
        ).rejects.toThrow(JwtVerificationError);
    });

    it("throws JwtVerificationError when the cookie's JWT fails verification", async () => {
        const token = await signToken();

        await expect(
            extractIdentity(requestWithCookie(token), {
                getJwks,
                expectedIssuer: "https://not-trainfree.cloudflareaccess.com",
                expectedAudience: AUDIENCE,
            }),
        ).rejects.toThrow(JwtVerificationError);
    });
});
