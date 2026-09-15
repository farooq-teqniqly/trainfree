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

// Thrown when the caller's audience env var isn't set -- a deployment/config gap, not
// an untrusted caller presenting a bad token. Kept distinct from JwtVerificationError
// so the request handler can log it and respond 503 ("could not evaluate") instead of
// 401 ("evaluated and rejected"), which would otherwise make a config outage
// indistinguishable from an ordinary invalid-token request.
export class AudienceNotConfiguredError extends Error {
    constructor() {
        super("No audience configured for this caller");
        this.name = "AudienceNotConfiguredError";
    }
}

// Verifies signature, issuer, expiry, and that `expectedAudience` is a member of the
// JWT's `aud` claim -- jose's own `audience` option checks array membership rather
// than strict equality against the whole claim, which is required since Cloudflare
// Access JWTs carry `aud` as an array. `exp` is required explicitly: jose only checks
// expiry when the claim is present, so an otherwise-valid token minted without one
// would never expire.
export async function verifyAccessJwt(token, { getJwks, expectedIssuer, expectedAudience }) {
    if (!expectedAudience) {
        throw new AudienceNotConfiguredError();
    }

    const jwks = await getJwks();
    const keySet = createLocalJWKSet(jwks);

    try {
        const { payload } = await jwtVerify(token, keySet, {
            issuer: expectedIssuer,
            audience: expectedAudience,
            requiredClaims: ["exp"],
        });
        return payload;
    } catch (err) {
        throw new JwtVerificationError(err);
    }
}
