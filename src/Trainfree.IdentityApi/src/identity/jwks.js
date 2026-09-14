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

        // Cache the response before reading its body, since Cache API stores the
        // response object as-provided and a consumed body would leave nothing to cache.
        await cache.put(cacheKey, response.clone());
        return response.json();
    };
}
