import { describe, expect, it } from "vitest";
import { env } from "cloudflare:test";
import { listProgramsTree } from "./programs-tree.js";
import { createProgram } from "./programs.js";
import { createSession } from "./sessions.js";
import { createPhase } from "./phases.js";
import { createExercise } from "./exercises.js";
import { createSessionPhase } from "./session-phases.js";
import { createProgramExercise } from "./program-exercises.js";

describe("listProgramsTree", () => {
    it("returns an empty array when there are no programs", async () => {
        const tree = await listProgramsTree(env.DB);

        expect(tree).toEqual([]);
    });

    it("includes a program with no sessions as an empty sessions array", async () => {
        await createProgram(env.DB, "Workout A");

        const tree = await listProgramsTree(env.DB);

        expect(tree).toHaveLength(1);
        expect(tree[0].sessions).toEqual([]);
    });

    it("includes a session with no phases as an empty phases array", async () => {
        const program = await createProgram(env.DB, "Workout A");
        await createSession(env.DB, program.id, "Monday Lower Body");

        const tree = await listProgramsTree(env.DB);

        expect(tree[0].sessions).toHaveLength(1);
        expect(tree[0].sessions[0].phases).toEqual([]);
    });

    it("includes a session phase with no exercises as an empty exercises array", async () => {
        const program = await createProgram(env.DB, "Workout A");
        const session = await createSession(env.DB, program.id, "Monday Lower Body");
        const phase = await createPhase(env.DB, "Warm Up");
        await createSessionPhase(env.DB, session.id, phase.id);

        const tree = await listProgramsTree(env.DB);

        const sessionPhases = tree[0].sessions[0].phases;
        expect(sessionPhases).toHaveLength(1);
        expect(sessionPhases[0].phaseId).toBe(phase.id);
        expect(sessionPhases[0].exercises).toEqual([]);
    });

    it("groups a fully populated tree under the correct parents", async () => {
        const programA = await createProgram(env.DB, "Workout A");
        const programB = await createProgram(env.DB, "Workout B");
        const sessionA = await createSession(env.DB, programA.id, "Monday Lower Body");
        const sessionB = await createSession(env.DB, programB.id, "Tuesday Upper Body");
        const phase = await createPhase(env.DB, "Warm Up");
        const exercise = await createExercise(env.DB, "Squat");
        const sessionPhaseA = await createSessionPhase(env.DB, sessionA.id, phase.id);
        await createSessionPhase(env.DB, sessionB.id, phase.id);
        await createProgramExercise(env.DB, sessionPhaseA.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            weight: 45,
            sets: 3,
            restSeconds: 60,
            side: "Both",
        });

        const tree = await listProgramsTree(env.DB);

        expect(tree.map((p) => p.name)).toEqual(["Workout A", "Workout B"]);
        const [treeA, treeB] = tree;
        expect(treeA.sessions).toHaveLength(1);
        expect(treeA.sessions[0].phases).toHaveLength(1);
        expect(treeA.sessions[0].phases[0].exercises).toHaveLength(1);
        expect(treeA.sessions[0].phases[0].exercises[0].exerciseId).toBe(exercise.id);
        expect(treeB.sessions).toHaveLength(1);
        expect(treeB.sessions[0].phases).toHaveLength(1);
        expect(treeB.sessions[0].phases[0].exercises).toEqual([]);
    });
});
