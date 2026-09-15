import { SELF } from "cloudflare:test";
import { describe, expect, it } from "vitest";
import { versionStamp } from "./version.js";

describe("versionStamp", () => {
    it("falls back to a local-build stamp when the deploy vars are absent", () => {
        expect(versionStamp({})).toEqual({ version: "local", commit: "local" });
    });
});

describe("GET /api/version", () => {
    it("reports the version and commit stamped in at deploy time", async () => {
        const response = await SELF.fetch("http://worker/api/version");

        expect(response.status).toBe(200);
        expect(response.headers.get("content-type")).toContain("application/json");
        expect(await response.json()).toEqual({ version: "v9.9.9", commit: "abc1234" });
    });

    it("never allows the response to be cached", async () => {
        const response = await SELF.fetch("http://worker/api/version");

        expect(response.headers.get("cache-control")).toBe("no-store");
    });

    it("rejects methods other than GET with the same stable JSON error shape", async () => {
        const response = await SELF.fetch("http://worker/api/version", { method: "POST" });

        expect(response.status).toBe(405);
        expect(response.headers.get("content-type")).toContain("application/json");
        expect(await response.json()).toEqual({ error: expect.any(String) });
    });
});

describe("unmatched routes", () => {
    it("responds 404 with the same stable JSON error shape as every other non-200 response", async () => {
        const response = await SELF.fetch("http://worker/no-such-route");

        expect(response.status).toBe(404);
        expect(response.headers.get("content-type")).toContain("application/json");
        expect(await response.json()).toEqual({ error: "not found" });
    });

    it("responds 404 with the same JSON shape for a non-GET request to /internal/identity", async () => {
        const response = await SELF.fetch("http://worker/internal/identity", { method: "POST" });

        expect(response.status).toBe(404);
        expect(await response.json()).toEqual({ error: "not found" });
    });
});
