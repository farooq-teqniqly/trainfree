import { describe, expect, it, vi } from "vitest";
import { createJwksFetcher } from "./jwks.js";

const fakeJwks = { keys: [{ kty: "RSA", kid: "test-key", n: "abc", e: "AQAB" }] };

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
