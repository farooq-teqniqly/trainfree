import { beforeEach, describe, expect, it } from "vitest";
import { env } from "cloudflare:test";
import { createProgram } from "./programs.js";
import { createSession } from "./sessions.js";
import { createPhase } from "./phases.js";
import { createExercise } from "./exercises.js";
import { createSessionPhase } from "./session-phases.js";
import {
    createProgramExercise,
    deleteProgramExercise,
    getProgramExerciseType,
    listProgramExercises,
    programExerciseSessionPhaseExists,
    updateProgramExercise,
} from "./program-exercises.js";

describe("programExerciseSessionPhaseExists", () => {
    it("returns true for a session phase that exists under that session", async () => {
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");
        const phase = await createPhase(env.DB, "Warm Up");
        const sessionPhase = await createSessionPhase(env.DB, session.id, phase.id);

        const result = await programExerciseSessionPhaseExists(
            env.DB,
            session.id,
            sessionPhase.id,
        );

        expect(result).toBe(true);
    });

    it("returns false for a sessionPhaseId that does not exist", async () => {
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");

        const result = await programExerciseSessionPhaseExists(env.DB, session.id, "SPH-ZZZZZZ");

        expect(result).toBe(false);
    });

    it("returns false for a sessionPhaseId that exists under a different session", async () => {
        const program = await createProgram(env.DB, "Workout A");
        const sessionA = await createSession(env.DB, program.id, "Monday Lower Body");
        const sessionB = await createSession(env.DB, program.id, "Wednesday Upper Body");
        const phase = await createPhase(env.DB, "Warm Up");
        const sessionPhase = await createSessionPhase(env.DB, sessionA.id, phase.id);

        const result = await programExerciseSessionPhaseExists(
            env.DB,
            sessionB.id,
            sessionPhase.id,
        );

        expect(result).toBe(false);
    });
});

describe("listProgramExercises", () => {
    let session;
    let sessionPhase;
    let exercise;

    beforeEach(async () => {
        const program = await createProgram(env.DB, "Workout A");
        session = await createSession(env.DB, program.id, "Monday Lower Body");
        const phase = await createPhase(env.DB, "Warm Up");
        sessionPhase = await createSessionPhase(env.DB, session.id, phase.id);
        exercise = await createExercise(env.DB, "Bodyweight Squat");
    });

    it("returns an empty array when the session phase has no program exercises", async () => {
        const result = await listProgramExercises(env.DB, sessionPhase.id);

        expect(result).toEqual([]);
    });

    it("returns the session phase's program exercises in creation order", async () => {
        const first = await createProgramExercise(env.DB, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });
        const second = await createProgramExercise(env.DB, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Timed",
            durationSeconds: 30,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });

        const result = await listProgramExercises(env.DB, sessionPhase.id);

        expect(result.map((pe) => pe.id)).toEqual([first.id, second.id]);
    });

    it("excludes program exercises belonging to a different session phase", async () => {
        const otherPhase = await createSessionPhase(env.DB, session.id, sessionPhase.phaseId);
        await createProgramExercise(env.DB, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });
        await createProgramExercise(env.DB, otherPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });

        const result = await listProgramExercises(env.DB, sessionPhase.id);

        expect(result).toHaveLength(1);
    });
});

describe("createProgramExercise", () => {
    let sessionPhase;
    let exercise;

    beforeEach(async () => {
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");
        const phase = await createPhase(env.DB, "Warm Up");
        sessionPhase = await createSessionPhase(env.DB, session.id, phase.id);
        exercise = await createExercise(env.DB, "Bodyweight Squat");
    });

    it("creates a Reps program exercise with a generated id and no durationSeconds property", async () => {
        const result = await createProgramExercise(env.DB, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });

        expect(result).toEqual({
            id: expect.stringMatching(/^PGX-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/),
            sessionPhaseId: sessionPhase.id,
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
            createdAt: expect.any(String),
            updatedAt: expect.any(String),
        });
        expect(result).not.toHaveProperty("durationSeconds");
    });

    it("creates a Timed program exercise with a generated id and no reps property", async () => {
        const result = await createProgramExercise(env.DB, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Timed",
            durationSeconds: 30,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });

        expect(result).toEqual({
            id: expect.stringMatching(/^PGX-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/),
            sessionPhaseId: sessionPhase.id,
            exerciseId: exercise.id,
            type: "Timed",
            durationSeconds: 30,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
            createdAt: expect.any(String),
            updatedAt: expect.any(String),
        });
        expect(result).not.toHaveProperty("reps");
    });

    it("allows a second row for the same exerciseId under the same session phase", async () => {
        await createProgramExercise(env.DB, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });

        const result = await createProgramExercise(env.DB, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 8,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });

        expect(result.id).toBeTypeOf("string");
        const list = await listProgramExercises(env.DB, sessionPhase.id);
        expect(list).toHaveLength(2);
    });
});

describe("getProgramExerciseType", () => {
    it("returns the row's type when it exists under that session phase", async () => {
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");
        const phase = await createPhase(env.DB, "Warm Up");
        const sessionPhase = await createSessionPhase(env.DB, session.id, phase.id);
        const exercise = await createExercise(env.DB, "Bodyweight Squat");
        const created = await createProgramExercise(env.DB, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Timed",
            durationSeconds: 30,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });

        const result = await getProgramExerciseType(env.DB, sessionPhase.id, created.id);

        expect(result).toBe("Timed");
    });

    it("returns null for an id that does not exist under that session phase", async () => {
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");
        const phase = await createPhase(env.DB, "Warm Up");
        const sessionPhase = await createSessionPhase(env.DB, session.id, phase.id);

        const result = await getProgramExerciseType(env.DB, sessionPhase.id, "PGX-ZZZZZZ");

        expect(result).toBeNull();
    });
});

describe("updateProgramExercise", () => {
    let sessionPhase;
    let exercise;
    let created;

    beforeEach(async () => {
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");
        const phase = await createPhase(env.DB, "Warm Up");
        sessionPhase = await createSessionPhase(env.DB, session.id, phase.id);
        exercise = await createExercise(env.DB, "Bodyweight Squat");
        created = await createProgramExercise(env.DB, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });
    });

    it("updates the provided fields and returns the updated row", async () => {
        const result = await updateProgramExercise(env.DB, sessionPhase.id, created.id, {
            reps: 12,
            weight: 45,
        });

        expect(result.reps).toBe(12);
        expect(result.weight).toBe(45);
        expect(result.sets).toBe(3);
    });

    it("returns null for an id that does not exist under that session phase", async () => {
        const result = await updateProgramExercise(env.DB, sessionPhase.id, "PGX-ZZZZZZ", {
            weight: 45,
        });

        expect(result).toBeNull();
    });

    it("returns null when the program exercise belongs to a different session phase", async () => {
        const otherSessionPhase = await createSessionPhase(
            env.DB,
            sessionPhase.sessionId,
            sessionPhase.phaseId,
        );

        const result = await updateProgramExercise(env.DB, otherSessionPhase.id, created.id, {
            weight: 45,
        });

        expect(result).toBeNull();
    });
});

describe("deleteProgramExercise", () => {
    let sessionPhase;
    let exercise;
    let created;

    beforeEach(async () => {
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");
        const phase = await createPhase(env.DB, "Warm Up");
        sessionPhase = await createSessionPhase(env.DB, session.id, phase.id);
        exercise = await createExercise(env.DB, "Bodyweight Squat");
        created = await createProgramExercise(env.DB, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });
    });

    it("returns true and removes the row for an existing program exercise", async () => {
        const result = await deleteProgramExercise(env.DB, sessionPhase.id, created.id);

        expect(result).toBe(true);
        expect(await listProgramExercises(env.DB, sessionPhase.id)).toEqual([]);
    });

    it("returns false for an id that does not exist", async () => {
        const result = await deleteProgramExercise(env.DB, sessionPhase.id, "PGX-ZZZZZZ");

        expect(result).toBe(false);
    });

    it("returns false for an id belonging to a different session phase", async () => {
        const otherSessionPhase = await createSessionPhase(
            env.DB,
            sessionPhase.sessionId,
            sessionPhase.phaseId,
        );

        const result = await deleteProgramExercise(env.DB, otherSessionPhase.id, created.id);

        expect(result).toBe(false);
        expect(await listProgramExercises(env.DB, sessionPhase.id)).toHaveLength(1);
    });
});
