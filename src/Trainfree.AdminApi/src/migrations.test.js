import { applyD1Migrations, env } from "cloudflare:test";
import { describe, expect, it } from "vitest";

describe("0008_copy_categories_to_phases migration", () => {
    it("copies an existing categories row into phases with the PHS- prefix", async () => {
        await env.DB.prepare(
            "INSERT INTO categories (category_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
            .bind("CAT-AB2345", "Warm Up", "2026-08-31T00:00:00.000Z", "2026-08-31T00:00:00.000Z")
            .run();
        await env.DB.prepare(
            "DELETE FROM d1_migrations WHERE name = ?",
        )
            .bind("0008_copy_categories_to_phases.sql")
            .run();

        await applyD1Migrations(env.DB, env.TEST_MIGRATIONS);

        const phase = await env.DB.prepare(
            "SELECT phase_id, name, created_at, updated_at FROM phases WHERE phase_id = ?",
        )
            .bind("PHS-AB2345")
            .first();
        expect(phase).toEqual({
            phase_id: "PHS-AB2345",
            name: "Warm Up",
            created_at: "2026-08-31T00:00:00.000Z",
            updated_at: "2026-08-31T00:00:00.000Z",
        });
        const category = await env.DB.prepare(
            "SELECT category_id FROM categories WHERE category_id = ?",
        )
            .bind("CAT-AB2345")
            .first();
        expect(category).not.toBeNull();
    });
});
