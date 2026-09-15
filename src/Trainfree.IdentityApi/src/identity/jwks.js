import { importJWK } from "jose";

// A forced refresh (see `getJwks`) is triggered by an untrusted input -- a JWT's `kid`
// claim -- so an attacker presenting a stream of tokens with distinct, never-valid
// `kid` values could otherwise force an upstream fetch on every single request,
// bypassing the normal cache entirely. This per-`certsUrl` cooldown collapses repeated
// forced refreshes within a short window into one real fetch; requests arriving during
// the cooldown fall back to the normal (possibly cache-hit) path instead, so they still
// get correctly rejected once, just without each one hitting the certs endpoint.
// Module-scope state: only bounds the amplification within a single Worker isolate,
// not globally, since nothing here uses a shared external store -- consistent with the
// per-isolate nature of the Cache API this file already relies on.
const FORCED_REFRESH_COOLDOWN_MS = 5000;
const lastForcedRefreshAt = new Map();

function forcedRefreshAllowed(certsUrl) {
    const last = lastForcedRefreshAt.get(certsUrl) ?? 0;
    if (Date.now() - last < FORCED_REFRESH_COOLDOWN_MS) {
        return false;
    }
    lastForcedRefreshAt.set(certsUrl, Date.now());
    return true;
}

// `createLocalJWKSet` (jose) selects a key by matching the JWT header's `kid` against
// each key's own `kid` -- a key with no `kid` (or a non-string one) can import
// successfully yet can never actually be selected for a real Cloudflare Access token,
// which always carries one. Filtering on this before attempting the import avoids
// caching a "usable" key that is structurally importable but functionally dead weight.
function hasSelectableKeyId(key) {
    return !!key && typeof key.kty === "string" && typeof key.kid === "string" && key.kid.length > 0;
}

// A JWKS document needs at least one usable key entry to ever verify anything -- an
// empty `keys` array is structurally valid JSON but useless, and would otherwise pass
// an `Array.isArray` check and get cached (or served from cache) as if it were a real
// key set. `kty`/`kid` alone isn't enough either: a key with both present but missing
// its actual key material (e.g. an RSA entry with no `n`/`e`) still passes that shape
// check, but `createLocalJWKSet` can't import it, so tokens selecting it fail anyway --
// just later, and without this function ever having rejected the response. `importJWK`
// is jose's own key-import routine, so asking it to import every candidate is a direct
// test of "can this actually verify a token," not a hand-rolled re-check of jose's
// internal requirements.
//
// Filters out unusable keys rather than rejecting the whole document: Cloudflare Access
// can publish multiple keys at once (e.g. during key rotation), and one
// malformed/unsupported entry must not take every other, genuinely valid key offline
// with it. Returns `null` only when *no* key in the set is usable; otherwise returns the
// usable subset plus whether anything was actually dropped, so the caller can decide
// whether the result is safe to cache for the full upstream lifetime (see `getJwks`).
async function sanitizeJwks(jwks) {
    if (!jwks || !Array.isArray(jwks.keys) || jwks.keys.length === 0) {
        return null;
    }

    const candidates = jwks.keys.filter(hasSelectableKeyId);
    const results = await Promise.allSettled(candidates.map((key) => importJWK(key, key?.alg)));
    const usableKeys = candidates.filter((_key, index) => results[index].status === "fulfilled");
    if (usableKeys.length === 0) {
        return null;
    }

    return { jwks: { ...jwks, keys: usableKeys }, complete: usableKeys.length === jwks.keys.length };
}

// Fetches Cloudflare Access's JWKS and caches it via the Cache API, honoring the
// response's own Cache-Control/max-age instead of inventing a second freshness
// policy. `fetcher` is an injectable seam so tests can supply a fake JWKS response
// with no real network call; `cache` defaults to the Worker's `caches.default`.
export function createJwksFetcher({ certsUrl, fetcher = fetch, cache = caches.default }) {
    const cacheKey = new Request(certsUrl);

    return async function getJwks({ forceRefresh = false } = {}) {
        // `forceRefresh` skips straight to the upstream fetch -- used when a caller
        // already knows the cached set doesn't have the key it needs (e.g. jwt.js
        // retrying after a JWKSNoMatchingKey failure), so there's no point checking the
        // cache again first. Subject to a cooldown (see forcedRefreshAllowed) so it
        // can't be used to force an upstream fetch on every single request.
        const effectiveForceRefresh = forceRefresh && forcedRefreshAllowed(certsUrl);
        if (!effectiveForceRefresh) {
            const cached = await cache.match(cacheKey);
            if (cached) {
                // A cached entry can't be un-cached from here (the Cache API has no
                // atomic "invalidate and refetch"), but re-validating on every hit at
                // least turns a previously-cached-but-now-invalid or corrupted document
                // into an immediate, retryable error rather than a silent, prolonged
                // authentication outage with no obvious cause.
                const cachedJwks = await cached.json().catch(() => null);
                const sanitized = await sanitizeJwks(cachedJwks);
                if (sanitized) {
                    return sanitized.jwks;
                }
            }
        }

        const response = await fetcher(certsUrl);
        if (!response.ok) {
            throw new Error(`JWKS fetch failed with status ${response.status}`);
        }

        // Validate before caching -- caching an HTTP-success response whose body isn't a
        // usable JWKS document would turn one bad upstream response into an outage for
        // the response's entire cache lifetime, since every subsequent call would keep
        // serving the poisoned cache entry instead of retrying upstream.
        const jwks = await response.json().catch(() => null);
        const sanitized = await sanitizeJwks(jwks);
        if (!sanitized) {
            throw new Error("JWKS response did not contain a valid, non-empty keys array");
        }

        // An incomplete set (some keys dropped) still gets cached, but only for a short,
        // bounded lifetime rather than the full upstream one -- caching it for the whole
        // lifetime would mean a key that gets corrected upstream (e.g. a rotation key
        // that was briefly malformed) stays invisible to this Worker until the stale
        // entry expires, but not caching it at all means every single identity check
        // reaches the certs endpoint for as long as the upstream document persistently
        // contains one bad entry alongside otherwise-valid keys. The short TTL bounds
        // both problems: valid traffic mostly hits the cache, and a correction is
        // visible again within a minute rather than a full cache lifetime.
        const cacheControl = sanitized.complete
            ? (response.headers.get("cache-control") ?? "")
            : "public, max-age=60";
        await cache.put(
            cacheKey,
            new Response(JSON.stringify(sanitized.jwks), {
                headers: {
                    "content-type": "application/json",
                    "cache-control": cacheControl,
                },
            }),
        );
        return sanitized.jwks;
    };
}
