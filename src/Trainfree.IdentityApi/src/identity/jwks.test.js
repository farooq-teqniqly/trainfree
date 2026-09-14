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
});
