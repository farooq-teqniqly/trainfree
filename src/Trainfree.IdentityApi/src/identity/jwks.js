import { importJWK } from "jose";

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
        // cache again first.
        if (!forceRefresh) {
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

        // Only cache a complete set. Caching an incomplete one (some keys dropped) for
        // the full upstream cache lifetime would mean a key that gets corrected upstream
        // -- e.g. a rotation key that was briefly malformed -- stays invisible to this
        // Worker until the stale entry expires, since nothing here re-checks a cached
        // entry against the live upstream on its own; better to keep refetching until a
        // clean response arrives.
        if (sanitized.complete) {
            await cache.put(
                cacheKey,
                new Response(JSON.stringify(sanitized.jwks), {
                    headers: {
                        "content-type": "application/json",
                        "cache-control": response.headers.get("cache-control") ?? "",
                    },
                }),
            );
        }
        return sanitized.jwks;
    };
}
