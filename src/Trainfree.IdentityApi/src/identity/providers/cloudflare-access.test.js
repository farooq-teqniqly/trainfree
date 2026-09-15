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

async function signToken(email = "user@example.com") {
    return new SignJWT(email === null ? {} : { email })
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
