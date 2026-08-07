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

    it("rejects methods other than GET", async () => {
        const response = await SELF.fetch("http://worker/api/version", { method: "POST" });

        expect(response.status).toBe(405);
    });
});
