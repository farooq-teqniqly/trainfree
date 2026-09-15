import { JwtVerificationError, verifyAccessJwt } from "../jwt.js";

// The only place in this Worker allowed to read the CF_Authorization cookie or
// Access-specific JWT claims (email/aud/iss) -- every other call site goes through
// `extractIdentity` and gets back a plain identity value, never the raw claims.
function cfAuthorizationCookie(request) {
    const cookieHeader = request.headers.get("Cookie");
    if (!cookieHeader) {
        return null;
    }

    const match = cookieHeader.match(/(?:^|;\s*)CF_Authorization=([^;]+)/);
    return match ? match[1] : null;
}

// Extracts and verifies the Cloudflare Access identity from an incoming request,
// returning `{ email }`. Throws `JwtVerificationError` (re-exported by `jwt.js`) when
// the cookie is missing or the token fails verification.
export async function extractIdentity(request, { getJwks, expectedIssuer, expectedAudience }) {
    const token = cfAuthorizationCookie(request);
    if (!token) {
        throw new JwtVerificationError(new Error("Missing CF_Authorization cookie"));
    }

    const payload = await verifyAccessJwt(token, { getJwks, expectedIssuer, expectedAudience });
    if (typeof payload.email !== "string" || payload.email.length === 0) {
        throw new JwtVerificationError(new Error("JWT is missing a valid email claim"));
    }

    return { email: payload.email };
}
