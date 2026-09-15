import { applyD1Migrations, env } from "cloudflare:test";
import { describe, expect, it } from "vitest";

async function insertProgram(db, { programId, userId }) {
    return db
        .prepare(
            "INSERT INTO programs (program_id, name, created_at, updated_at, user_id) VALUES (?, ?, ?, ?, ?)",
        )
        .bind(programId, "Test Program", "2026-09-15T00:00:00.000Z", "2026-09-15T00:00:00.000Z", userId)
        .run();
}

describe("0015_add_programs_user_id migration", () => {
    it("adds a nullable user_id column that a pre-existing/owner-less program row keeps as NULL", async () => {
        await insertProgram(env.DB, { programId: "PGM-NOOWNER1", userId: null });

        const row = await env.DB.prepare("SELECT user_id FROM programs WHERE program_id = ?")
            .bind("PGM-NOOWNER1")
            .first();

        expect(row).toEqual({ user_id: null });
    });

    it("enforces the user_id foreign key against users(user_id) -- an unknown value is rejected", async () => {
        await expect(insertProgram(env.DB, { programId: "PGM-BADOWNER1", userId: "USR-NOTREAL" })).rejects.toThrow();
    });

    it("accepts a user_id that matches a real users row", async () => {
        const now = "2026-09-15T00:00:00.000Z";
        const loginResult = await env.DB.prepare(
            "INSERT INTO logins (provider_name, provider_id, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
            .bind("cloudflare-access", "migration-test-owner@example.com", now, now)
            .run();
        await env.DB.prepare(
            "INSERT INTO users (user_id, login_id, role_id, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
        )
            .bind("USR-MIGTEST1", loginResult.meta.last_row_id, "ROL-A3F7K2", now, now)
            .run();

        await insertProgram(env.DB, { programId: "PGM-REALOWNER1", userId: "USR-MIGTEST1" });

        const row = await env.DB.prepare("SELECT user_id FROM programs WHERE program_id = ?")
            .bind("PGM-REALOWNER1")
            .first();
        expect(row).toEqual({ user_id: "USR-MIGTEST1" });
    });
});

describe("0009_drop_categories migration", () => {
    it("drops the categories table", async () => {
        const table = await env.DB.prepare(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'categories'",
        ).first();

        expect(table).toBeNull();
    });

    it("leaves the phases row that 0008 copied from categories intact after the drop", async () => {
        await env.DB.prepare("DELETE FROM d1_migrations WHERE name = ?")
            .bind("0004_create_categories.sql")
            .run();
        await env.DB.prepare("DELETE FROM d1_migrations WHERE name = ?")
            .bind("0005_add_categories_name_unique_index.sql")
            .run();
        await applyD1Migrations(env.DB, env.TEST_MIGRATIONS);

        await env.DB.prepare(
            "INSERT INTO categories (category_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
            .bind("CAT-ZZ9999", "Cooldown", "2026-09-04T00:00:00.000Z", "2026-09-04T00:00:00.000Z")
            .run();

        await env.DB.prepare("DELETE FROM d1_migrations WHERE name = ?")
            .bind("0008_copy_categories_to_phases.sql")
            .run();
        await env.DB.prepare("DELETE FROM d1_migrations WHERE name = ?")
            .bind("0009_drop_categories.sql")
            .run();
        await applyD1Migrations(env.DB, env.TEST_MIGRATIONS);

        const phase = await env.DB.prepare(
            "SELECT phase_id, name, created_at, updated_at FROM phases WHERE phase_id = ?",
        )
            .bind("PHS-ZZ9999")
            .first();
        expect(phase).toEqual({
            phase_id: "PHS-ZZ9999",
            name: "Cooldown",
            created_at: "2026-09-04T00:00:00.000Z",
            updated_at: "2026-09-04T00:00:00.000Z",
        });

        const table = await env.DB.prepare(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'categories'",
        ).first();
        expect(table).toBeNull();
    });
});
