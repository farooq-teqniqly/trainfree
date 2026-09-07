import { describe, expect, it } from "vitest";
import { env } from "cloudflare:test";
import { createPhase, deletePhase } from "./phases.js";
import { createProgram } from "./programs.js";
import { createSession } from "./sessions.js";
import { createSessionPhase } from "./session-phases.js";
import { PhaseInUseError } from "./errors.js";

describe("deletePhase", () => {
    it("throws PhaseInUseError and makes no change when the phase is referenced by a session", async () => {
        const phase = await createPhase(env.DB, "Warm Up");
        const program = await createProgram(env.DB, "Workout A");
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
});
