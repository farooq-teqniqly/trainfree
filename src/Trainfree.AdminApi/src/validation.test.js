import { describe, expect, it } from "vitest";
import {
    validateCreateProgramExercise,
    validateExerciseName,
    validatePhaseName,
    validateProgramName,
    validateSessionName,
    validateUpdateProgramExercise,
} from "./validation.js";

describe("validateProgramName", () => {
    it.each([
        ["exactly 4 chars", "Abcd"],
        ["exactly 100 chars", "A".repeat(100)],
        ["mid-range", "Monday Lower Body"],
        ["trims surrounding whitespace before measuring", "  Abcd  "],
    ])("accepts %s", (_label, name) => {
        const result = validateProgramName(name);

        expect(result.valid).toBe(true);
    });

    it.each([
        ["missing", undefined],
        ["null", null],
        ["empty", ""],
        ["whitespace-only", "    "],
        ["3 chars", "Abc"],
        ["101 chars", "A".repeat(101)],
    ])("rejects %s", (_label, name) => {
        const result = validateProgramName(name);

        expect(result.valid).toBe(false);
        expect(result.error).toBeTypeOf("string");
    });

    it("returns the trimmed name on success", () => {
        const result = validateProgramName("  Monday Lower Body  ");

        expect(result.valid).toBe(true);
        expect(result.name).toBe("Monday Lower Body");
    });
});

describe("validateSessionName", () => {
    it.each([
        ["exactly 4 chars", "Abcd"],
        ["exactly 100 chars", "A".repeat(100)],
        ["mid-range", "Monday Lower Body"],
        ["trims surrounding whitespace before measuring", "  Abcd  "],
    ])("accepts %s", (_label, name) => {
        const result = validateSessionName(name);

        expect(result.valid).toBe(true);
    });

    it.each([
        ["missing", undefined],
        ["null", null],
        ["empty", ""],
        ["whitespace-only", "    "],
        ["3 chars", "Abc"],
        ["101 chars", "A".repeat(101)],
    ])("rejects %s", (_label, name) => {
        const result = validateSessionName(name);

        expect(result.valid).toBe(false);
        expect(result.error).toBeTypeOf("string");
    });
});

describe("validateExerciseName", () => {
    it.each([
        ["exactly 4 chars", "Abcd"],
        ["exactly 100 chars", "A".repeat(100)],
        ["mid-range", "Bodyweight Squat"],
        ["trims surrounding whitespace before measuring", "  Abcd  "],
    ])("accepts %s", (_label, name) => {
        const result = validateExerciseName(name);

        expect(result.valid).toBe(true);
    });

    it.each([
        ["missing", undefined],
        ["null", null],
        ["empty", ""],
        ["whitespace-only", "    "],
        ["3 chars", "Abc"],
        ["101 chars", "A".repeat(101)],
    ])("rejects %s", (_label, name) => {
        const result = validateExerciseName(name);

        expect(result.valid).toBe(false);
        expect(result.error).toBeTypeOf("string");
    });
});

describe("validatePhaseName", () => {
    it.each([
        ["exactly 4 chars", "Abcd"],
        ["exactly 100 chars", "A".repeat(100)],
        ["mid-range", "Warm Up"],
        ["trims surrounding whitespace before measuring", "  Abcd  "],
    ])("accepts %s", (_label, name) => {
        const result = validatePhaseName(name);

        expect(result.valid).toBe(true);
    });

    it.each([
        ["missing", undefined],
        ["null", null],
        ["empty", ""],
        ["whitespace-only", "    "],
        ["3 chars", "Abc"],
        ["101 chars", "A".repeat(101)],
    ])("rejects %s", (_label, name) => {
        const result = validatePhaseName(name);

        expect(result.valid).toBe(false);
        expect(result.error).toBeTypeOf("string");
    });
});

describe("validateCreateProgramExercise", () => {
    it("accepts a valid Reps body", () => {
        const result = validateCreateProgramExercise({
            exerciseId: "EXR-AAAAAA",
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
        });

        expect(result).toEqual({
            valid: true,
            exerciseId: "EXR-AAAAAA",
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });
    });

    it("accepts a valid Timed body", () => {
        const result = validateCreateProgramExercise({
            exerciseId: "EXR-AAAAAA",
            type: "Timed",
            durationSeconds: 30,
            sets: 3,
            restSeconds: 60,
        });

        expect(result).toEqual({
            valid: true,
            exerciseId: "EXR-AAAAAA",
            type: "Timed",
            durationSeconds: 30,
            sets: 3,
            restSeconds: 60,
            weight: 0,
            side: "Both",
        });
    });

    it("carries an explicit weight and side through when provided", () => {
        const result = validateCreateProgramExercise({
            exerciseId: "EXR-AAAAAA",
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 45.5,
            side: "Left",
        });

        expect(result.valid).toBe(true);
        expect(result.weight).toBe(45.5);
        expect(result.side).toBe("Left");
    });

    it.each([[undefined], [null], [""], [123]])(
        "rejects a missing or non-string exerciseId %s",
        (exerciseId) => {
            const result = validateCreateProgramExercise({
                exerciseId,
                type: "Reps",
                reps: 10,
                sets: 3,
                restSeconds: 60,
            });

            expect(result.valid).toBe(false);
            expect(result.error).toBeTypeOf("string");
        },
    );

    it.each([[undefined], [null], [""], ["Bogus"]])(
        "rejects a missing or invalid type %s",
        (type) => {
            const result = validateCreateProgramExercise({
                exerciseId: "EXR-AAAAAA",
                type,
                reps: 10,
                sets: 3,
                restSeconds: 60,
            });

            expect(result.valid).toBe(false);
            expect(result.error).toBeTypeOf("string");
        },
    );

    it.each([[undefined], [null], [0], [-1], [1.5]])(
        "rejects a missing, zero, negative, or non-integer reps %s for type Reps",
        (reps) => {
            const result = validateCreateProgramExercise({
                exerciseId: "EXR-AAAAAA",
                type: "Reps",
                reps,
                sets: 3,
                restSeconds: 60,
            });

            expect(result.valid).toBe(false);
        },
    );

    it.each([[undefined], [null], [0], [-1], [1.5]])(
        "rejects a missing, zero, negative, or non-integer durationSeconds %s for type Timed",
        (durationSeconds) => {
            const result = validateCreateProgramExercise({
                exerciseId: "EXR-AAAAAA",
                type: "Timed",
                durationSeconds,
                sets: 3,
                restSeconds: 60,
            });

            expect(result.valid).toBe(false);
        },
    );

    it.each([[undefined], [null], [0], [-1]])(
        "rejects a missing, zero, or negative sets %s",
        (sets) => {
            const result = validateCreateProgramExercise({
                exerciseId: "EXR-AAAAAA",
                type: "Reps",
                reps: 10,
                sets,
                restSeconds: 60,
            });

            expect(result.valid).toBe(false);
        },
    );

    it.each([[undefined], [null], [0], [-1]])(
        "rejects a missing, zero, or negative restSeconds %s",
        (restSeconds) => {
            const result = validateCreateProgramExercise({
                exerciseId: "EXR-AAAAAA",
                type: "Reps",
                reps: 10,
                sets: 3,
                restSeconds,
            });

            expect(result.valid).toBe(false);
        },
    );

    it("rejects a Reps body that also carries durationSeconds", () => {
        const result = validateCreateProgramExercise({
            exerciseId: "EXR-AAAAAA",
            type: "Reps",
            reps: 10,
            durationSeconds: 30,
            sets: 3,
            restSeconds: 60,
        });

        expect(result.valid).toBe(false);
    });

    it("rejects a Timed body that also carries reps", () => {
        const result = validateCreateProgramExercise({
            exerciseId: "EXR-AAAAAA",
            type: "Timed",
            durationSeconds: 30,
            reps: 10,
            sets: 3,
            restSeconds: 60,
        });

        expect(result.valid).toBe(false);
    });

    it("rejects a negative weight", () => {
        const result = validateCreateProgramExercise({
            exerciseId: "EXR-AAAAAA",
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: -1,
        });

        expect(result.valid).toBe(false);
    });

    it("accepts a zero weight", () => {
        const result = validateCreateProgramExercise({
            exerciseId: "EXR-AAAAAA",
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: 0,
        });

        expect(result.valid).toBe(true);
    });

    it("rejects a side that is not Both/Left/Right", () => {
        const result = validateCreateProgramExercise({
            exerciseId: "EXR-AAAAAA",
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            side: "Up",
        });

        expect(result.valid).toBe(false);
    });
});

describe("validateUpdateProgramExercise", () => {
    it("accepts a partial update to a single field", () => {
        const result = validateUpdateProgramExercise({ weight: 50 });

        expect(result).toEqual({ valid: true, updates: { weight: 50 } });
    });

    it("accepts an update to every editable field", () => {
        const result = validateUpdateProgramExercise({
            reps: 12,
            weight: 50,
            sets: 4,
            restSeconds: 90,
            side: "Right",
        });

        expect(result).toEqual({
            valid: true,
            updates: { reps: 12, weight: 50, sets: 4, restSeconds: 90, side: "Right" },
        });
    });

    it("rejects a body containing exerciseId", () => {
        const result = validateUpdateProgramExercise({ exerciseId: "EXR-AAAAAA", weight: 50 });

        expect(result.valid).toBe(false);
        expect(result.error).toBeTypeOf("string");
    });

    it("rejects a body containing type", () => {
        const result = validateUpdateProgramExercise({ type: "Timed" });

        expect(result.valid).toBe(false);
        expect(result.error).toBeTypeOf("string");
    });

    it.each([[0], [-1], [1.5]])(
        "rejects a zero, negative, or non-integer reps %s",
        (reps) => {
            const result = validateUpdateProgramExercise({ reps });

            expect(result.valid).toBe(false);
        },
    );

    it.each([[0], [-1], [1.5]])(
        "rejects a zero, negative, or non-integer durationSeconds %s",
        (durationSeconds) => {
            const result = validateUpdateProgramExercise({ durationSeconds });

            expect(result.valid).toBe(false);
        },
    );

    it.each([[0], [-1]])("rejects a zero or negative sets %s", (sets) => {
        const result = validateUpdateProgramExercise({ sets });

        expect(result.valid).toBe(false);
    });

    it.each([[0], [-1]])("rejects a zero or negative restSeconds %s", (restSeconds) => {
        const result = validateUpdateProgramExercise({ restSeconds });

        expect(result.valid).toBe(false);
    });

    it("rejects a negative weight", () => {
        const result = validateUpdateProgramExercise({ weight: -1 });

        expect(result.valid).toBe(false);
    });

    it("accepts a zero weight", () => {
        const result = validateUpdateProgramExercise({ weight: 0 });

        expect(result.valid).toBe(true);
    });

    it("rejects a side that is not Both/Left/Right", () => {
        const result = validateUpdateProgramExercise({ side: "Up" });

        expect(result.valid).toBe(false);
    });
});
