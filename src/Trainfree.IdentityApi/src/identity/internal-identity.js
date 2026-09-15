import { jsonError } from "../shared/http.js";
import { PROVIDER_NAME_CLOUDFLARE_ACCESS } from "../shared/providers.js";
import { CALLER_CONFIG, certsUrlFor, issuerFor } from "./config.js";
import { createJwksFetcher } from "./jwks.js";
import { JwtVerificationError } from "./jwt.js";
import { extractIdentity } from "./provider.js";
import { lookupIdentity } from "./roles.js";

// Plain `===` short-circuits on the first differing byte, leaking a timing
// side-channel on the one secret that actually gates this publicly-reachable
// endpoint (see the comment above `handleInternalIdentity`). Compare every byte
// regardless of where they first differ.
function timingSafeEqual(a, b) {
    const aBytes = new TextEncoder().encode(a);
    const bBytes = new TextEncoder().encode(b);
    // A length mismatch still returns early, leaking key length via timing --
    // accepted, since both keys are fixed-length generated secrets (same
    // trade-off Node's own crypto.timingSafeEqual makes by requiring equal-length
    // buffers up front).
    if (aBytes.length !== bBytes.length) {
        return false;
    }
    let diff = 0;
    for (let i = 0; i < aBytes.length; i++) {
        diff |= aBytes[i] ^ bBytes[i];
    }
    return diff === 0;
}

// GET /internal/identity -- the only route AdminApi/WorkoutApi call over their service
// binding. `/internal/identity` is a naming convention only, not an access boundary:
// this Worker also has a public hostname, so the per-caller internal key is what
// actually restricts the endpoint. The key check therefore runs, and fails closed with
// 404, before the JWT is looked at at all -- see spec's "Missing or wrong internal key
// responds 404, checked before the JWT."
//
// `fetcher`/`db` are an injectable seam for tests (a fake JWKS fetch, a
// forced-throwing D1 stub); production calls omit them and get the real `fetch` and
// `env.DB`.
export async function handleInternalIdentity(request, env, { fetcher, db } = {}) {
    const callerName = request.headers.get("X-Trainfree-Caller");
    const caller = CALLER_CONFIG[callerName];
    if (!caller) {
        return jsonError("missing or invalid X-Trainfree-Caller", 401);
    }

    const presentedKey = request.headers.get("X-Trainfree-Internal-Key");
    const expectedKey = env[caller.internalKeyVar];
    if (!expectedKey || !presentedKey || !timingSafeEqual(presentedKey, expectedKey)) {
        return jsonError("not found", 404);
    }

    let identity;
    try {
        const getJwks = createJwksFetcher({
            certsUrl: certsUrlFor(env),
            fetcher: fetcher ?? fetch,
        });
        identity = await extractIdentity(request, {
            getJwks,
            expectedIssuer: issuerFor(env),
            expectedAudience: env[caller.audienceVar],
        });
    } catch (err) {
        if (err instanceof JwtVerificationError) {
            return jsonError("unauthorized", 401);
        }
        console.error("JWT verification infrastructure failure", err);
        return jsonError("service unavailable", 503);
    }

    let resolved;
    try {
        resolved = await lookupIdentity(db ?? env.DB, {
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            providerId: identity.email,
        });
    } catch (err) {
        console.error("D1 role lookup failed", err);
        return jsonError("service unavailable", 503);
    }

    if (!resolved?.role) {
        return jsonError("forbidden", 403);
    }

    return new Response(
        JSON.stringify({ email: identity.email, userId: resolved.userId, role: resolved.role }),
        { status: 200, headers: { "content-type": "application/json" } },
    );
}
