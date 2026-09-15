// Fetches Cloudflare Access's JWKS and caches it via the Cache API, honoring the
// response's own Cache-Control/max-age instead of inventing a second freshness
// policy. `fetcher` is an injectable seam so tests can supply a fake JWKS response
// with no real network call; `cache` defaults to the Worker's `caches.default`.
export function createJwksFetcher({ certsUrl, fetcher = fetch, cache = caches.default }) {
    const cacheKey = new Request(certsUrl);

    return async function getJwks() {
        const cached = await cache.match(cacheKey);
        if (cached) {
            return cached.json();
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
        if (!jwks || !Array.isArray(jwks.keys)) {
            throw new Error("JWKS response did not contain a valid keys array");
        }

        await cache.put(cacheKey, cacheable);
        return jwks;
    };
}
