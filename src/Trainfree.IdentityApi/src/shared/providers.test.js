import { describe, expect, it } from "vitest";
import { PROVIDER_NAME_CLOUDFLARE_ACCESS } from "./providers.js";

describe("providers", () => {
    it("PROVIDER_NAME_CLOUDFLARE_ACCESS_IsCloudflareAccess", () => {
        // Arrange / Act / Assert
        expect(PROVIDER_NAME_CLOUDFLARE_ACCESS).toBe("cloudflare-access");
    });
});
