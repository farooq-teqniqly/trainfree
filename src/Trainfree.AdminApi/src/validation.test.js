import { describe, expect, it } from "vitest";
import { validatePhaseName, validateProgramName, validateSessionName } from "./validation.js";

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
