import { describe, expect, it, vi } from "vitest";
import { env } from "cloudflare:test";
import { PROVIDER_NAME_CLOUDFLARE_ACCESS } from "../shared/providers.js";
import { provisionIdentity, UnknownRoleError } from "./provisioning.js";

const ADMINISTRATOR_ROLE_NAME = "Administrator";

async function seedIdentity(db, { providerName, providerId, roleId }) {
    const now = new Date().toISOString();
    const loginResult = await db
        .prepare(
            "INSERT INTO logins (provider_name, provider_id, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
        .bind(providerName, providerId, now, now)
        .run();
    await db
        .prepare(
            "INSERT INTO users (user_id, login_id, role_id, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
        )
        .bind("USR-EXIST01", loginResult.meta.last_row_id, roleId, now, now)
        .run();
}

describe("provisionIdentity", () => {
    it("makes no D1 writes when a fully provisioned identity already exists", async () => {
        // Arrange
        const providerId = "already-provisioned@example.com";
        await seedIdentity(env.DB, {
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            providerId,
            roleId: "ROL-A3F7K2",
        });
        const batchSpy = vi.spyOn(env.DB, "batch");

        // Act
        const result = await provisionIdentity(env.DB, {
            email: providerId,
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            roleName: ADMINISTRATOR_ROLE_NAME,
        });

        // Assert
        expect(result).toEqual({ created: false });
        expect(batchSpy).not.toHaveBeenCalled();
    });

    it("inserts the logins and users rows in a single db.batch() call when no identity exists yet", async () => {
        // Arrange
        const email = "new-identity@example.com";
        const batchSpy = vi.spyOn(env.DB, "batch");

        // Act
        const result = await provisionIdentity(env.DB, {
            email,
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            roleName: ADMINISTRATOR_ROLE_NAME,
        });

        // Assert
        expect(result.created).toBe(true);
        expect(batchSpy).toHaveBeenCalledTimes(1);
        expect(batchSpy.mock.calls[0][0]).toHaveLength(2);

        const row = await env.DB.prepare(
            `SELECT users.user_id as userId, roles.name as role
             FROM logins
             JOIN users ON users.login_id = logins.id
             LEFT JOIN roles ON roles.role_id = users.role_id
             WHERE logins.provider_name = ? AND logins.provider_id = ?`,
        )
            .bind(PROVIDER_NAME_CLOUDFLARE_ACCESS, email)
            .first();
        expect(row).toEqual({ userId: result.userId, role: ADMINISTRATOR_ROLE_NAME });
    });

    it("throws UnknownRoleError and writes nothing when the role name does not exist", async () => {
        // Arrange
        const email = "unknown-role@example.com";
        const batchSpy = vi.spyOn(env.DB, "batch");

        // Act / Assert
        await expect(
            provisionIdentity(env.DB, {
                email,
                providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
                roleName: "NotARole",
            }),
        ).rejects.toThrow(UnknownRoleError);
        expect(batchSpy).not.toHaveBeenCalled();
    });
});
