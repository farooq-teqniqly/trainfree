import { env } from "cloudflare:test";
import { describe, expect, it } from "vitest";

const APP_TABLES = [
    "exercises",
    "phases",
    "program_exercises",
    "programs",
    "session_phases",
    "sessions",
];

const NOW = "2026-10-24T00:00:00.000Z";

describe("baseline schema", () => {
    it.each(APP_TABLES)("creates the %s table", async (table) => {
        const row = await env.DB.prepare(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name = ?",
        )
            .bind(table)
            .first();

        expect(row).toEqual({ name: table });
    });

    it.each(["logins", "users", "roles", "categories"])("does not create the %s table", async (table) => {
        const row = await env.DB.prepare(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name = ?",
        )
            .bind(table)
            .first();

        expect(row).toBeNull();
    });

    it("leaves programs.user_id without a foreign key", async () => {
        const { results } = await env.DB.prepare("PRAGMA foreign_key_list(programs)").all();

        expect(results).toEqual([]);
    });

    it("accepts a program whose user_id names no users row", async () => {
        await env.DB.prepare(
            "INSERT INTO programs (program_id, name, user_id, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
        )
            .bind("PGM-BASELINE1", "Baseline Program", "USR-ANYONE", NOW, NOW)
            .run();

        const row = await env.DB.prepare("SELECT user_id as userId FROM programs WHERE program_id = ?")
            .bind("PGM-BASELINE1")
            .first();
        expect(row).toEqual({ userId: "USR-ANYONE" });
    });

    it("rejects an insert into programs with a NULL user_id", async () => {
        await expect(
            env.DB.prepare(
                "INSERT INTO programs (program_id, name, user_id, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
            )
                .bind("PGM-NULLOWNER1", "No Owner", null, NOW, NOW)
                .run(),
        ).rejects.toThrow(/user_id must not be NULL/);
    });

    it("rejects an insert into programs that omits user_id", async () => {
        await expect(
            env.DB.prepare(
                "INSERT INTO programs (program_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
            )
                .bind("PGM-NOOWNER1", "No Owner", NOW, NOW)
                .run(),
        ).rejects.toThrow(/user_id must not be NULL/);
    });

    it("rejects an update that sets programs.user_id to NULL", async () => {
        await env.DB.prepare(
            "INSERT INTO programs (program_id, name, user_id, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
        )
            .bind("PGM-UPDNULL1", "Owned", "USR-ANYONE", NOW, NOW)
            .run();

        await expect(
            env.DB.prepare("UPDATE programs SET user_id = NULL WHERE program_id = ?")
                .bind("PGM-UPDNULL1")
                .run(),
        ).rejects.toThrow(/user_id must not be NULL/);
    });

    it("allows renaming a program without touching user_id", async () => {
        await env.DB.prepare(
            "INSERT INTO programs (program_id, name, user_id, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
        )
            .bind("PGM-RENAME1", "Before", "USR-ANYONE", NOW, NOW)
            .run();

        await env.DB.prepare("UPDATE programs SET name = ? WHERE program_id = ?")
            .bind("After", "PGM-RENAME1")
            .run();

        const row = await env.DB.prepare("SELECT name FROM programs WHERE program_id = ?")
            .bind("PGM-RENAME1")
            .first();
        expect(row).toEqual({ name: "After" });
    });

    it("cascades a program delete through sessions, session phases and program exercises", async () => {
        await env.DB.batch([
            env.DB.prepare(
                "INSERT INTO programs (program_id, name, user_id, created_at, updated_at) VALUES ('PGM-CASC1', 'P', 'USR-ANYONE', ?, ?)",
            ).bind(NOW, NOW),
            env.DB.prepare(
                "INSERT INTO sessions (session_id, program_id, name, created_at, updated_at) VALUES ('SES-CASC1', 'PGM-CASC1', 'S', ?, ?)",
            ).bind(NOW, NOW),
            env.DB.prepare(
                "INSERT INTO phases (phase_id, name, created_at, updated_at) VALUES ('PHS-CASC1', 'Ph', ?, ?)",
            ).bind(NOW, NOW),
            env.DB.prepare(
                "INSERT INTO session_phases (session_phase_id, session_id, phase_id, created_at) VALUES ('SPH-CASC1', 'SES-CASC1', 'PHS-CASC1', ?)",
            ).bind(NOW),
            env.DB.prepare(
                "INSERT INTO exercises (exercise_id, name, created_at, updated_at) VALUES ('EXR-CASC1', 'E', ?, ?)",
            ).bind(NOW, NOW),
            env.DB.prepare(
                `INSERT INTO program_exercises
                    (program_exercise_id, session_phase_id, exercise_id, type, reps, weight, sets, rest_seconds, side, created_at, updated_at)
                 VALUES ('PEX-CASC1', 'SPH-CASC1', 'EXR-CASC1', 'Reps', 10, 45, 3, 60, 'Both', ?, ?)`,
            ).bind(NOW, NOW),
        ]);

        await env.DB.prepare("DELETE FROM programs WHERE program_id = 'PGM-CASC1'").run();

        const counts = await env.DB.prepare(
            `SELECT (SELECT COUNT(*) FROM sessions) as sessions,
                    (SELECT COUNT(*) FROM session_phases) as sessionPhases,
                    (SELECT COUNT(*) FROM program_exercises) as programExercises`,
        ).first();
        expect(counts).toEqual({ sessions: 0, sessionPhases: 0, programExercises: 0 });
    });

    it("adds a nullable image_key column to exercises", async () => {
        await env.DB.prepare(
            "INSERT INTO exercises (exercise_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
            .bind("EXR-IMGKEY1", "No Image", NOW, NOW)
            .run();

        const row = await env.DB.prepare("SELECT image_key as imageKey FROM exercises WHERE exercise_id = ?")
            .bind("EXR-IMGKEY1")
            .first();
        expect(row).toEqual({ imageKey: null });
    });
});
