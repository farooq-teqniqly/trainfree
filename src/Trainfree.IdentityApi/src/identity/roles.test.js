import { describe, expect, it } from "vitest";
import { env } from "cloudflare:test";
import { PROVIDER_NAME_CLOUDFLARE_ACCESS } from "../shared/providers.js";
import { lookupIdentity } from "./roles.js";

const ADMINISTRATOR_ROLE_ID = "ROL-A3F7K2";
const USER_ROLE_ID = "ROL-Q8Z4M6";

async function seedLogin(db, { providerName, providerId }) {
    const now = new Date().toISOString();
    const result = await db
        .prepare(
            "INSERT INTO logins (provider_name, provider_id, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
        .bind(providerName, providerId, now, now)
        .run();
    return result.meta.last_row_id;
}

async function seedUser(db, { userId, loginId, roleId }) {
    const now = new Date().toISOString();
    await db
        .prepare(
            "INSERT INTO users (user_id, login_id, role_id, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
        )
        .bind(userId, loginId, roleId, now, now)
        .run();
}

describe("lookupIdentity", () => {
    it("resolves the role from the logins row matching provider_name specifically, not a same-provider_id row under a different provider_name", async () => {
        const sharedProviderId = "user@example.com";

        const otherLoginId = await seedLogin(env.DB, {
            providerName: "other-provider",
            providerId: sharedProviderId,
        });
        await seedUser(env.DB, {
            userId: "USR-OTHER01",
            loginId: otherLoginId,
            roleId: USER_ROLE_ID,
        });

        const accessLoginId = await seedLogin(env.DB, {
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            providerId: sharedProviderId,
        });
        await seedUser(env.DB, {
            userId: "USR-ACCESS01",
            loginId: accessLoginId,
            roleId: ADMINISTRATOR_ROLE_ID,
        });

        const identity = await lookupIdentity(env.DB, {
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            providerId: sharedProviderId,
        });

        expect(identity).toEqual({ userId: "USR-ACCESS01", role: "Administrator" });
    });

    it("reflects a role changed in D1 between two lookups, with no caching", async () => {
        const providerId = "role-change@example.com";
        const loginId = await seedLogin(env.DB, {
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            providerId,
        });
        await seedUser(env.DB, {
            userId: "USR-ROLECHG1",
            loginId,
            roleId: USER_ROLE_ID,
        });

        const firstLookup = await lookupIdentity(env.DB, {
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            providerId,
        });
        expect(firstLookup.role).toBe("User");

        await env.DB.prepare("UPDATE users SET role_id = ? WHERE user_id = ?")
            .bind(ADMINISTRATOR_ROLE_ID, "USR-ROLECHG1")
            .run();

        const secondLookup = await lookupIdentity(env.DB, {
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            providerId,
        });
        expect(secondLookup.role).toBe("Administrator");
    });

    it("returns null when no logins row matches the provider_name and provider_id pair", async () => {
        const identity = await lookupIdentity(env.DB, {
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            providerId: "unknown@example.com",
        });

        expect(identity).toBeNull();
    });
});
