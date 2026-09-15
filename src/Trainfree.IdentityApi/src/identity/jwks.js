// A JWKS document needs at least one usable key entry to ever verify anything -- an
// empty `keys` array is structurally valid JSON but useless, and would otherwise pass
// the `Array.isArray` check below and get cached (or served from cache) as if it were
// a real key set. `kty`/`kid` are the two fields `createLocalJWKSet` (jose) requires
// to select a key; a well-formed key missing either is unusable the same way.
function isUsableJwks(jwks) {
    return (
        !!jwks &&
        Array.isArray(jwks.keys) &&
        jwks.keys.length > 0 &&
        jwks.keys.every((key) => key && typeof key.kty === "string" && typeof key.kid === "string")
    );
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
            if (isUsableJwks(cachedJwks)) {
                return cachedJwks;
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
        const cacheable = response.clone();
        const jwks = await response.json().catch(() => null);
        if (!isUsableJwks(jwks)) {
            throw new Error("JWKS response did not contain a valid, non-empty keys array");
        }

        await cache.put(cacheKey, cacheable);
        return jwks;
    };
}
