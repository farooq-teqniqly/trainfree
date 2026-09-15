import { importJWK } from "jose";

// A JWKS document needs at least one usable key entry to ever verify anything -- an
// empty `keys` array is structurally valid JSON but useless, and would otherwise pass
// an `Array.isArray` check and get cached (or served from cache) as if it were a real
// key set. Checking `kty`/`kid` alone isn't enough either: a key with both present but
// missing its actual key material (e.g. an RSA entry with no `n`/`e`) still passes that
// shape check, but `createLocalJWKSet` can't import it, so tokens selecting it fail
// anyway -- just later, and without this function ever having rejected the response.
// `importJWK` is jose's own key-import routine, so asking it to import every key is a
// direct test of "can this actually verify a token," not a hand-rolled re-check of
// jose's internal requirements.
//
// Filters out unimportable keys rather than rejecting the whole document: Cloudflare
// Access can publish multiple keys at once (e.g. during key rotation), and one
// malformed/unsupported entry must not take every other, genuinely valid key offline
// with it. Returns `null` only when *no* key in the set is usable.
async function sanitizeJwks(jwks) {
    if (!jwks || !Array.isArray(jwks.keys) || jwks.keys.length === 0) {
        return null;
    }

    const results = await Promise.allSettled(jwks.keys.map((key) => importJWK(key, key?.alg)));
    const usableKeys = jwks.keys.filter((_key, index) => results[index].status === "fulfilled");
    if (usableKeys.length === 0) {
        return null;
    }

    return { ...jwks, keys: usableKeys };
}

// Fetches Cloudflare Access's JWKS and caches it via the Cache API, honoring the
// response's own Cache-Control/max-age instead of inventing a second freshness
// policy. `fetcher` is an injectable seam so tests can supply a fake JWKS response
// with no real network call; `cache` defaults to the Worker's `caches.default`.
export function createJwksFetcher({ certsUrl, fetcher = fetch, cache = caches.default }) {
    const cacheKey = new Request(certsUrl);

    return async function getJwks() {
        const cached = await cache.match(cacheKey);
        if (cached) {
            const cachedJwks = await cached.json();
            // A cached entry can't be un-cached from here (the Cache API has no atomic
            // "invalidate and refetch"), but re-validating on every hit at least turns a
            // previously-cached-but-now-invalid document into an immediate, retryable
            // error rather than a silent, prolonged authentication outage that looks
            // like "every JWT is rejected" with no obvious cause.
            const sanitized = await sanitizeJwks(cachedJwks);
            if (sanitized) {
                return sanitized;
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

        // Cache the sanitized set, not the raw response -- caching the original body
        // would re-attempt (and re-fail) importing the same unusable key on every future
        // cache hit's re-validation, for no benefit over dropping it once here.
        await cache.put(
            cacheKey,
            new Response(JSON.stringify(sanitized), {
                headers: {
                    "content-type": "application/json",
                    "cache-control": response.headers.get("cache-control") ?? "",
                },
            }),
        );
        return sanitized;
    };
}
