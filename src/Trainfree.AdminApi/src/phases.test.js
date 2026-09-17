import { describe, expect, it } from "vitest";
import { env } from "cloudflare:test";
import { createPhase, deletePhase } from "./phases.js";
import { createProgram } from "./programs.js";
import { createSession } from "./sessions.js";
import { createSessionPhase } from "./session-phases.js";
import { PhaseInUseError } from "./errors.js";
import { runAfterQueryResolves } from "./test-helpers.js";

// test/apply-migrations.js seeds this local-dev user for every test file's D1 instance.
const SEEDED_USER_ID = "USR-LOCALDEV";

describe("deletePhase", () => {
    it("throws PhaseInUseError and makes no change when the phase is referenced by a session", async () => {
        const phase = await createPhase(env.DB, "Warm Up");
        const program = await createProgram(env.DB, "Workout A", SEEDED_USER_ID);
        const session = await createSession(env.DB, program.id, "Monday Lower Body");
        await createSessionPhase(env.DB, session.id, phase.id);

        await expect(deletePhase(env.DB, phase.id)).rejects.toBeInstanceOf(PhaseInUseError);

        const row = await env.DB.prepare("SELECT 1 FROM phases WHERE phase_id = ?")
            .bind(phase.id)
            .first();
        expect(row).not.toBeNull();
    });

    it("deletes and returns true for an unreferenced phase", async () => {
        const phase = await createPhase(env.DB, "Warm Up");

        const result = await deletePhase(env.DB, phase.id);

        expect(result).toBe(true);
        const row = await env.DB.prepare("SELECT 1 FROM phases WHERE phase_id = ?")
            .bind(phase.id)
            .first();
        expect(row).toBeNull();
    });

    it("throws PhaseInUseError and makes no change when a session phase referencing it is created between the in-use check and the delete (issue #89 delete-wins race)", async () => {
        // Simulates a session phase landing right after deletePhase's own in-use check
        // sees no rows, but before its DELETE runs -- the DELETE itself must then hit
        // the session_phases.phase_id FK constraint and translate it to PhaseInUseError.
        const phase = await createPhase(env.DB, "Warm Up");
        const program = await createProgram(env.DB, "Workout A", SEEDED_USER_ID);
        const session = await createSession(env.DB, program.id, "Monday Lower Body");
        const racingDb = runAfterQueryResolves(
            env.DB,
            "SELECT 1 FROM session_phases WHERE phase_id = ?",
            () => createSessionPhase(env.DB, session.id, phase.id),
        );

        await expect(deletePhase(racingDb, phase.id)).rejects.toBeInstanceOf(PhaseInUseError);

        const row = await env.DB.prepare("SELECT 1 FROM phases WHERE phase_id = ?")
            .bind(phase.id)
            .first();
        expect(row).not.toBeNull();
    });
});
