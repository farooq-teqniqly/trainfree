import { describe, expect, it } from "vitest";
import { env } from "cloudflare:test";
import { createExercise, deleteExercise } from "./exercises.js";
import { createProgram } from "./programs.js";
import { createSession } from "./sessions.js";
import { createPhase } from "./phases.js";
import { createSessionPhase } from "./session-phases.js";
import { createProgramExercise } from "./program-exercises.js";
import { ExerciseInUseError } from "./errors.js";

describe("deleteExercise", () => {
    it("throws ExerciseInUseError and makes no change when the exercise is referenced by a program exercise", async () => {
        const exercise = await createExercise(env.DB, "Bodyweight Squat");
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");
        const phase = await createPhase(env.DB, "Warm Up");
        const sessionPhase = await createSessionPhase(env.DB, session.id, phase.id);
        await createProgramExercise(env.DB, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });

        await expect(deleteExercise(env.DB, exercise.id)).rejects.toBeInstanceOf(
            ExerciseInUseError,
        );

        const row = await env.DB.prepare("SELECT 1 FROM exercises WHERE exercise_id = ?")
            .bind(exercise.id)
            .first();
        expect(row).not.toBeNull();
    });

    it("deletes and returns true for an unreferenced exercise", async () => {
        const exercise = await createExercise(env.DB, "Bodyweight Squat");

        const result = await deleteExercise(env.DB, exercise.id);

        expect(result).toBe(true);
        const row = await env.DB.prepare("SELECT 1 FROM exercises WHERE exercise_id = ?")
            .bind(exercise.id)
            .first();
        expect(row).toBeNull();
    });
});
