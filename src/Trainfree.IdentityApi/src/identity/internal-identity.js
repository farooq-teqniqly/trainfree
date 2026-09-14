import { PROVIDER_NAME_CLOUDFLARE_ACCESS } from "../shared/providers.js";
import { CALLER_CONFIG, certsUrlFor, issuerFor } from "./config.js";
import { createJwksFetcher } from "./jwks.js";
import { JwtVerificationError } from "./jwt.js";
import { extractIdentity } from "./provider.js";
import { lookupIdentity } from "./roles.js";

function jsonError(message, status) {
    return new Response(JSON.stringify({ error: message }), {
        status,
        headers: { "content-type": "application/json" },
    });
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
    if (!expectedKey || !presentedKey || presentedKey !== expectedKey) {
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
        return jsonError("service unavailable", 503);
    }

    let resolved;
    try {
        resolved = await lookupIdentity(db ?? env.DB, {
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            providerId: identity.email,
        });
    } catch {
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
