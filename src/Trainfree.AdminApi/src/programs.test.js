import { env } from "cloudflare:test";
import { describe, expect, it } from "vitest";
import { createProgram } from "./programs.js";

// programs.user_id is a foreign key into users(user_id), so the passed value must name a
// real row -- test/apply-migrations.js seeds exactly this one for every test file.
const SEEDED_USER_ID = "USR-LOCALDEV";

describe("createProgram", () => {
    it("sets user_id on the inserted row to the passed value", async () => {
        await createProgram(env.DB, "Workout A", SEEDED_USER_ID);

        const row = await env.DB.prepare("SELECT user_id as userId FROM programs WHERE name = ?")
            .bind("Workout A")
            .first();
        expect(row.userId).toBe(SEEDED_USER_ID);
    });

    it("throws when no userId is passed", async () => {
        await expect(createProgram(env.DB, "Workout A")).rejects.toThrow(/non-empty userId/);
    });

    it("throws when userId is an empty string", async () => {
        await expect(createProgram(env.DB, "Workout A", "")).rejects.toThrow(/non-empty userId/);
    });
});
