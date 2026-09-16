import { exportJWK, generateKeyPair } from "jose";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { createJwksFetcher } from "./jwks.js";

// A real, importable RSA public key -- sanitizeJwks now actually imports each key via
// jose's importJWK, so a placeholder like `{ n: "abc", e: "AQAB" }` (not real key
// material) would fail that import and make every "valid JWKS" test fail too.
let fakeJwks;

beforeAll(async () => {
    const { publicKey } = await generateKeyPair("RS256");
    const publicJwk = await exportJWK(publicKey);
    publicJwk.kid = "test-key";
    publicJwk.alg = "RS256";
    publicJwk.use = "sig";
    fakeJwks = { keys: [publicJwk] };
});

function fakeJwksResponse(maxAgeSeconds = 3600) {
    return new Response(JSON.stringify(fakeJwks), {
        headers: {
            "content-type": "application/json",
            "cache-control": `public, max-age=${maxAgeSeconds}`,
        },
    });
}

describe("createJwksFetcher", () => {
    it("fetches and caches the JWKS when no cached entry exists", async () => {
        const fetcher = vi.fn().mockResolvedValue(fakeJwksResponse());
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/absent",
            fetcher,
        });

        const jwks = await getJwks();

        expect(jwks).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(1);
    });

    it("serves the cached JWKS within its max-age without fetching again", async () => {
        const fetcher = vi.fn().mockResolvedValue(fakeJwksResponse());
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/fresh",
            fetcher,
        });

        await getJwks();
        const secondResult = await getJwks();

        expect(secondResult).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(1);
    });

    it("throws when the fetch fails and no cached copy is available", async () => {
        const fetcher = vi.fn().mockResolvedValue(new Response("boom", { status: 500 }));
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/failing",
            fetcher,
        });

        await expect(getJwks()).rejects.toThrow(/JWKS fetch failed/);
    });

    it("does not cache a 200 response with a malformed body, and retries upstream on the next call", async () => {
        const fetcher = vi
            .fn()
            .mockResolvedValueOnce(new Response("not json", { status: 200 }))
            .mockResolvedValueOnce(fakeJwksResponse());
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/malformed",
            fetcher,
        });

        await expect(getJwks()).rejects.toThrow(/valid, non-empty keys array/);
        const jwks = await getJwks();

        expect(jwks).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(2);
    });

    it("does not cache a 200 response with an empty keys array, and retries upstream on the next call", async () => {
        const fetcher = vi
            .fn()
            .mockResolvedValueOnce(
                new Response(JSON.stringify({ keys: [] }), {
                    headers: { "content-type": "application/json", "cache-control": "public, max-age=3600" },
                }),
            )
            .mockResolvedValueOnce(fakeJwksResponse());
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/empty-keys",
            fetcher,
        });

        await expect(getJwks()).rejects.toThrow(/valid, non-empty keys array/);
        const jwks = await getJwks();

        expect(jwks).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(2);
    });

    it("does not cache a key entry missing kty/kid, and retries upstream on the next call", async () => {
        const fetcher = vi
            .fn()
            .mockResolvedValueOnce(
                new Response(JSON.stringify({ keys: [{ n: "abc", e: "AQAB" }] }), {
                    headers: { "content-type": "application/json", "cache-control": "public, max-age=3600" },
                }),
            )
            .mockResolvedValueOnce(fakeJwksResponse());
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/unusable-key",
            fetcher,
        });

        await expect(getJwks()).rejects.toThrow(/valid, non-empty keys array/);
        const jwks = await getJwks();

        expect(jwks).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(2);
    });

    it("does not cache a key with kty/kid present but unimportable key material, and retries upstream", async () => {
        // kty/kid alone aren't proof a key works -- an RSA entry missing its actual
        // modulus/exponent passes that shape check but jose can't import it.
        const unusableKey = { kty: "RSA", kid: "test-key" };
        const fetcher = vi
            .fn()
            .mockResolvedValueOnce(
                new Response(JSON.stringify({ keys: [unusableKey] }), {
                    headers: { "content-type": "application/json", "cache-control": "public, max-age=3600" },
                }),
            )
            .mockResolvedValueOnce(fakeJwksResponse());
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/unimportable-key",
            fetcher,
        });

        await expect(getJwks()).rejects.toThrow(/valid, non-empty keys array/);
        const jwks = await getJwks();

        expect(jwks).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(2);
    });

    it("keeps the valid keys and drops only the unusable one from a mixed response, caching it with a short TTL", async () => {
        const unusableKey = { kty: "RSA", kid: "bad-key" };
        const fetcher = vi.fn().mockResolvedValue(
            new Response(JSON.stringify({ keys: [unusableKey, ...fakeJwks.keys] }), {
                headers: { "content-type": "application/json", "cache-control": "public, max-age=3600" },
            }),
        );
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/mixed-keys",
            fetcher,
        });

        const jwks = await getJwks();
        const secondResult = await getJwks();

        expect(jwks.keys).toEqual(fakeJwks.keys);
        // Cached (with a short, bounded TTL, not the upstream's full max-age) rather than
        // refetched on every call -- otherwise a persistently-bad entry alongside
        // otherwise-valid keys would force every single identity check to hit the certs
        // endpoint.
        expect(secondResult.keys).toEqual(fakeJwks.keys);
        expect(fetcher).toHaveBeenCalledTimes(1);
    });

    it("drops a key with no kid even though it imports successfully", async () => {
        const keyWithoutKid = { ...fakeJwks.keys[0] };
        delete keyWithoutKid.kid;
        const fetcher = vi
            .fn()
            .mockResolvedValueOnce(
                new Response(JSON.stringify({ keys: [keyWithoutKid] }), {
                    headers: { "content-type": "application/json", "cache-control": "public, max-age=3600" },
                }),
            )
            .mockResolvedValueOnce(fakeJwksResponse());
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/no-kid",
            fetcher,
        });

        await expect(getJwks()).rejects.toThrow(/valid, non-empty keys array/);
        const jwks = await getJwks();

        expect(jwks).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(2);
    });

    it("treats a corrupted cached entry as a cache miss and refetches", async () => {
        const certsUrl = "https://example.cloudflareaccess.com/cdn-cgi/access/certs/corrupted-cache";
        await caches.default.put(
            new Request(certsUrl),
            new Response("not json", { headers: { "content-type": "application/json" } }),
        );
        const fetcher = vi.fn().mockResolvedValue(fakeJwksResponse());
        const getJwks = createJwksFetcher({ certsUrl, fetcher });

        const jwks = await getJwks();

        expect(jwks).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(1);
    });

    it("skips the cache entirely and refetches when forceRefresh is set", async () => {
        const fetcher = vi.fn().mockImplementation(async () => fakeJwksResponse());
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/force-refresh",
            fetcher,
        });

        await getJwks();
        await getJwks({ forceRefresh: true });
        expect(fetcher).toHaveBeenCalledTimes(2);
    });

    it("collapses repeated forceRefresh calls within the cooldown window into a single fetch", async () => {
        const fetcher = vi.fn().mockImplementation(async () => fakeJwksResponse());
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/refresh-cooldown",
            fetcher,
        });

        await getJwks({ forceRefresh: true });
        const secondResult = await getJwks({ forceRefresh: true });

        // The second call, arriving within the cooldown, shares the first call's
        // already-settled outcome instead of triggering a second upstream fetch.
        expect(secondResult).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(1);
    });

    it("shares one fetch and result across concurrent forceRefresh callers instead of each racing a fetch", async () => {
        let resolveFetch;
        const fetcher = vi.fn().mockImplementation(
            () =>
                new Promise((resolve) => {
                    resolveFetch = resolve;
                }),
        );
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/concurrent-refresh",
            fetcher,
        });

        const first = getJwks({ forceRefresh: true });
        const second = getJwks({ forceRefresh: true });
        resolveFetch(fakeJwksResponse());
        const [firstResult, secondResult] = await Promise.all([first, second]);

        expect(firstResult).toEqual(fakeJwks);
        expect(secondResult).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(1);
    });

    it("shares a failed forceRefresh's error with a concurrent caller instead of one falling back to a stale cache", async () => {
        const certsUrl = "https://example.cloudflareaccess.com/cdn-cgi/access/certs/concurrent-refresh-failure";
        // Seed a stale-but-still-structurally-valid cached entry -- the bug this test
        // guards against is a second caller silently falling back to exactly this kind
        // of stale data instead of seeing the refresh failure the first caller hit.
        await caches.default.put(new Request(certsUrl), fakeJwksResponse());
        let rejectFetch;
        const fetcher = vi.fn().mockImplementation(
            () =>
                new Promise((_resolve, reject) => {
                    rejectFetch = reject;
                }),
        );
        const getJwks = createJwksFetcher({ certsUrl, fetcher });

        const first = getJwks({ forceRefresh: true });
        const second = getJwks({ forceRefresh: true });
        rejectFetch(new Error("JWKS fetch failed with status 500"));

        await expect(first).rejects.toThrow(/JWKS fetch failed/);
        await expect(second).rejects.toThrow(/JWKS fetch failed/);
        expect(fetcher).toHaveBeenCalledTimes(1);
    });

    it("re-fetches instead of serving a cached entry that is no longer a usable JWKS", async () => {
        const certsUrl = "https://example.cloudflareaccess.com/cdn-cgi/access/certs/stale-cache";
        // Bypasses createJwksFetcher's own validated write path to simulate a cache
        // entry that was valid when written but would no longer pass validation --
        // exactly the scenario re-validating on a cache hit exists to catch.
        await caches.default.put(
            new Request(certsUrl),
            new Response(JSON.stringify({ keys: [] }), {
                headers: { "content-type": "application/json", "cache-control": "public, max-age=3600" },
            }),
        );
        const fetcher = vi.fn().mockResolvedValue(fakeJwksResponse());
        const getJwks = createJwksFetcher({ certsUrl, fetcher });

        const jwks = await getJwks();

        expect(jwks).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(1);
    });

    it("still resolves the JWKS on every call when the upstream response carries no Cache-Control", async () => {
        const fetcher = vi.fn().mockImplementation(
            async () =>
                new Response(JSON.stringify(fakeJwks), {
                    headers: { "content-type": "application/json" },
                }),
        );
        const getJwks = createJwksFetcher({
            certsUrl: "https://example.cloudflareaccess.com/cdn-cgi/access/certs/no-cache-control",
            fetcher,
        });

        const firstResult = await getJwks();
        const secondResult = await getJwks();

        expect(firstResult).toEqual(fakeJwks);
        expect(secondResult).toEqual(fakeJwks);
        expect(fetcher).toHaveBeenCalledTimes(2);
    });
});
