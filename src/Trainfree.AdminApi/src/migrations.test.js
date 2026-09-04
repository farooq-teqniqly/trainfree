import { applyD1Migrations, env } from "cloudflare:test";
import { describe, expect, it } from "vitest";

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

        const phase = await env.DB.prepare("SELECT phase_id, name FROM phases WHERE phase_id = ?")
            .bind("PHS-ZZ9999")
            .first();
        expect(phase).toEqual({ phase_id: "PHS-ZZ9999", name: "Cooldown" });

        const table = await env.DB.prepare(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'categories'",
        ).first();
        expect(table).toBeNull();
    });
});
