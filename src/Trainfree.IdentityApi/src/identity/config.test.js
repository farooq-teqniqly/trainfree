import { describe, expect, it } from "vitest";
import { certsUrlFor, issuerFor } from "./config.js";

describe("issuerFor", () => {
    it("builds the Cloudflare Access issuer URL from the configured team domain", () => {
        expect(issuerFor({ ACCESS_TEAM_DOMAIN: "trainfree" })).toBe(
            "https://trainfree.cloudflareaccess.com",
        );
    });
});

describe("certsUrlFor", () => {
    it("builds the exact certs endpoint path Cloudflare Access publishes its JWKS at", () => {
        expect(certsUrlFor({ ACCESS_TEAM_DOMAIN: "trainfree" })).toBe(
            "https://trainfree.cloudflareaccess.com/cdn-cgi/access/certs",
        );
    });
});
