import { createLocalJWKSet, jwtVerify } from "jose";

// Wraps every jose verification failure (bad signature, wrong issuer, expired,
// audience not present) in one type so callers don't need to know jose's specific
// error classes -- they only need to know "this JWT did not verify."
export class JwtVerificationError extends Error {
    constructor(cause) {
        super("JWT verification failed", { cause });
        this.name = "JwtVerificationError";
    }
}

// Verifies signature, issuer, expiry, and that `expectedAudience` is a member of the
// JWT's `aud` claim -- jose's own `audience` option checks array membership rather
// than strict equality against the whole claim, which is required since Cloudflare
// Access JWTs carry `aud` as an array.
export async function verifyAccessJwt(token, { getJwks, expectedIssuer, expectedAudience }) {
    const jwks = await getJwks();
    const keySet = createLocalJWKSet(jwks);

    try {
        const { payload } = await jwtVerify(token, keySet, {
            issuer: expectedIssuer,
            audience: expectedAudience,
        });
        return payload;
    } catch (err) {
        throw new JwtVerificationError(err);
    }
}
