import { describe, expect, it, vi } from "vitest";
import { env } from "cloudflare:test";
import { PROVIDER_NAME_CLOUDFLARE_ACCESS } from "../shared/providers.js";
import { provisionIdentity, UnknownRoleError } from "./provisioning.js";

const ADMINISTRATOR_ROLE_NAME = "Administrator";

async function seedIdentity(db, { providerName, providerId, roleId }) {
    const now = new Date().toISOString();
    const loginResult = await seedOrphanLogin(db, { providerName, providerId });
    await db
        .prepare(
            "INSERT INTO users (user_id, login_id, role_id, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
        )
        .bind("USR-EXIST01", loginResult.meta.last_row_id, roleId, now, now)
        .run();
}

async function seedOrphanLogin(db, { providerName, providerId }) {
    const now = new Date().toISOString();
    return db
        .prepare(
            "INSERT INTO logins (provider_name, provider_id, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
        .bind(providerName, providerId, now, now)
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

    it("attaches a users row to an existing orphaned logins row instead of re-inserting logins", async () => {
        // Arrange
        const email = "orphan@example.com";
        await seedOrphanLogin(env.DB, { providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS, providerId: email });
        const batchSpy = vi.spyOn(env.DB, "batch");

        // Act
        const result = await provisionIdentity(env.DB, {
            email,
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            roleName: ADMINISTRATOR_ROLE_NAME,
        });

        // Assert
        expect(result.created).toBe(true);
        expect(batchSpy).not.toHaveBeenCalled();
        const loginCount = await env.DB.prepare(
            "SELECT COUNT(*) as count FROM logins WHERE provider_name = ? AND provider_id = ?",
        )
            .bind(PROVIDER_NAME_CLOUDFLARE_ACCESS, email)
            .first();
        expect(loginCount.count).toBe(1);
    });

    it("resolves to created:false when a concurrent call wins the race repairing the same orphan", async () => {
        // Arrange
        const email = "orphan-racing@example.com";
        const loginResult = await seedOrphanLogin(env.DB, {
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            providerId: email,
        });
        const originalPrepare = env.DB.prepare.bind(env.DB);
        const prepareSpy = vi.spyOn(env.DB, "prepare").mockImplementation((sql) => {
            if (!sql.startsWith("INSERT INTO users")) {
                return originalPrepare(sql);
            }
            return {
                bind: () => ({
                    run: async () => {
                        const now = new Date().toISOString();
                        await originalPrepare(
                            "INSERT INTO users (user_id, login_id, role_id, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
                        )
                            .bind("USR-WINNER1", loginResult.meta.last_row_id, "ROL-A3F7K2", now, now)
                            .run();
                        throw new Error("D1_ERROR: UNIQUE constraint failed: users.login_id");
                    },
                }),
            };
        });

        // Act
        const result = await provisionIdentity(env.DB, {
            email,
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            roleName: ADMINISTRATOR_ROLE_NAME,
        });

        // Assert
        expect(result).toEqual({ created: false });
        prepareSpy.mockRestore();
    });

    it("resolves to created:false when a concurrent call wins the race on the same login", async () => {
        // Arrange
        const email = "racing@example.com";
        const batchSpy = vi.spyOn(env.DB, "batch").mockImplementationOnce(async () => {
            await seedIdentity(env.DB, {
                providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
                providerId: email,
                roleId: "ROL-A3F7K2",
            });
            throw new Error("D1_ERROR: UNIQUE constraint failed: logins.provider_name, logins.provider_id");
        });

        // Act
        const result = await provisionIdentity(env.DB, {
            email,
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            roleName: ADMINISTRATOR_ROLE_NAME,
        });

        // Assert
        expect(result).toEqual({ created: false });
        batchSpy.mockRestore();
    });

    it("re-throws a db.batch() error that is not a unique-constraint violation", async () => {
        // Arrange
        const email = "infra-failure@example.com";
        const batchSpy = vi
            .spyOn(env.DB, "batch")
            .mockRejectedValueOnce(new Error("D1 connection reset"));

        // Act / Assert
        await expect(
            provisionIdentity(env.DB, {
                email,
                providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
                roleName: ADMINISTRATOR_ROLE_NAME,
            }),
        ).rejects.toThrow("D1 connection reset");
        batchSpy.mockRestore();
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
