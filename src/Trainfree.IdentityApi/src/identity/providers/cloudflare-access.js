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

// One-way, deterministic per-login id: the raw identity_nonce is a lookup key for
// Access's identity record and must never reach telemetry or a response. Not a
// documented-stable claim, so a missing/invalid one yields no id rather than a failure.
async function deriveSessionId(nonce) {
    if (typeof nonce !== "string" || nonce.length === 0) {
        return undefined;
    }

    const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(nonce));
    return Array.from(new Uint8Array(digest).slice(0, 16), (b) => b.toString(16).padStart(2, "0")).join("");
}

// Extracts and verifies the Cloudflare Access identity from an incoming request,
// returning `{ email }` plus `sessionId` when the token carries an `identity_nonce`. Throws `JwtVerificationError` (re-exported by `jwt.js`) when
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

    const sessionId = await deriveSessionId(payload.identity_nonce);
    return sessionId ? { email: payload.email, sessionId } : { email: payload.email };
}
