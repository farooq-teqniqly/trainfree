import { beforeEach, describe, expect, it } from "vitest";
import { env } from "cloudflare:test";
import { createProgram } from "./programs.js";
import { createSession } from "./sessions.js";
import { createPhase } from "./phases.js";
import {
    createSessionPhase,
    deleteSessionPhase,
    listSessionPhases,
    sessionExists,
} from "./session-phases.js";

describe("sessionExists", () => {
    it("returns true for a session that exists under that program", async () => {
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");

        const result = await sessionExists(env.DB, program.id, session.id);

        expect(result).toBe(true);
    });

    it("returns false for a sessionId that does not exist", async () => {
        const program = await createProgram(env.DB, "Workout A");

        const result = await sessionExists(env.DB, program.id, "SNN-ZZZZZZ");

        expect(result).toBe(false);
    });

    it("returns false for a sessionId that exists under a different program", async () => {
        const programA = await createProgram(env.DB, "Workout A");
        const programB = await createProgram(env.DB, "Workout B");
        const session = await createSession(env.DB, programA.id, "Monday Lower Body");

        const result = await sessionExists(env.DB, programB.id, session.id);

        expect(result).toBe(false);
    });
});

describe("listSessionPhases", () => {
    let program;
    let session;
    let phase;

    beforeEach(async () => {
        program = await createProgram(env.DB, "Workout A");
        session = await createSession(env.DB, program.id, "Monday Lower Body");
        phase = await createPhase(env.DB, "Warm Up");
    });

    it("returns an empty array when the session has no phases", async () => {
        const result = await listSessionPhases(env.DB, session.id);

        expect(result).toEqual([]);
    });

    it("returns the session's phases in creation order", async () => {
        const phase2 = await createPhase(env.DB, "Cool Down");
        await createSessionPhase(env.DB, session.id, phase.id);
        await createSessionPhase(env.DB, session.id, phase2.id);

        const result = await listSessionPhases(env.DB, session.id);

        expect(result.map((sp) => sp.phaseId)).toEqual([phase.id, phase2.id]);
    });

    it("breaks a created_at tie using insertion order", async () => {
        const tiedTimestamp = "2026-09-06T00:00:00.000Z";
        await env.DB.prepare(
            "INSERT INTO session_phases (session_phase_id, session_id, phase_id, created_at) VALUES (?, ?, ?, ?)",
        )
            .bind("SPH-ZZZZZZ", session.id, phase.id, tiedTimestamp)
            .run();
        await env.DB.prepare(
            "INSERT INTO session_phases (session_phase_id, session_id, phase_id, created_at) VALUES (?, ?, ?, ?)",
        )
            .bind("SPH-AAAAAA", session.id, phase.id, tiedTimestamp)
            .run();

        const result = await listSessionPhases(env.DB, session.id);

        expect(result.map((sp) => sp.id)).toEqual(["SPH-ZZZZZZ", "SPH-AAAAAA"]);
    });

    it("excludes session phases belonging to a different session", async () => {
        const otherSession = await createSession(env.DB, program.id, "Wednesday Upper Body");
        await createSessionPhase(env.DB, session.id, phase.id);
        await createSessionPhase(env.DB, otherSession.id, phase.id);

        const result = await listSessionPhases(env.DB, session.id);

        expect(result).toHaveLength(1);
    });

    it("returns { id, sessionId, phaseId, createdAt } shaped rows", async () => {
        await createSessionPhase(env.DB, session.id, phase.id);

        const [result] = await listSessionPhases(env.DB, session.id);

        expect(result).toEqual({
            id: expect.any(String),
            sessionId: session.id,
            phaseId: phase.id,
            createdAt: expect.any(String),
        });
    });
});

describe("createSessionPhase", () => {
    let program;
    let session;
    let phase;

    beforeEach(async () => {
        program = await createProgram(env.DB, "Workout A");
        session = await createSession(env.DB, program.id, "Monday Lower Body");
        phase = await createPhase(env.DB, "Warm Up");
    });

    it("creates a row with a generated id and returns it", async () => {
        const result = await createSessionPhase(env.DB, session.id, phase.id);

        expect(result).toEqual({
            id: expect.stringMatching(/^SPH-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/),
            sessionId: session.id,
            phaseId: phase.id,
            createdAt: expect.any(String),
        });
    });

    it("allows a second row with the same (sessionId, phaseId) pair", async () => {
        await createSessionPhase(env.DB, session.id, phase.id);
        await createSessionPhase(env.DB, session.id, phase.id);

        const result = await listSessionPhases(env.DB, session.id);

        expect(result).toHaveLength(2);
    });
});

describe("deleteSessionPhase", () => {
    it("returns true and removes the row for an existing session phase", async () => {
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");
        const phase = await createPhase(env.DB, "Warm Up");
        const sessionPhase = await createSessionPhase(env.DB, session.id, phase.id);

        const result = await deleteSessionPhase(env.DB, session.id, sessionPhase.id);

        expect(result).toBe(true);
        expect(await listSessionPhases(env.DB, session.id)).toEqual([]);
    });

    it("returns false for an id that does not exist", async () => {
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");

        const result = await deleteSessionPhase(env.DB, session.id, "SPH-ZZZZZZ");

        expect(result).toBe(false);
    });

    it("returns false for an id belonging to a different session", async () => {
        const program = await createProgram(env.DB, "Workout A");
        const sessionA = await createSession(env.DB, program.id, "Monday Lower Body");
        const sessionB = await createSession(env.DB, program.id, "Wednesday Upper Body");
        const phase = await createPhase(env.DB, "Warm Up");
        const sessionPhase = await createSessionPhase(env.DB, sessionA.id, phase.id);

        const result = await deleteSessionPhase(env.DB, sessionB.id, sessionPhase.id);

        expect(result).toBe(false);
        expect(await listSessionPhases(env.DB, sessionA.id)).toHaveLength(1);
    });
});
