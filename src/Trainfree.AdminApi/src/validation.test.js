import { describe, expect, it } from "vitest";
import { validateProgramName, validateSessionName } from "./validation.js";

describe("validateProgramName", () => {
    it.each([
        ["exactly 5 chars", "Abcde"],
        ["exactly 100 chars", "A".repeat(100)],
        ["mid-range", "Monday Lower Body"],
        ["trims surrounding whitespace before measuring", "  Abcde  "],
    ])("accepts %s", (_label, name) => {
        const result = validateProgramName(name);

        expect(result.valid).toBe(true);
    });

    it.each([
        ["missing", undefined],
        ["null", null],
        ["empty", ""],
        ["whitespace-only", "    "],
        ["4 chars", "Abcd"],
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
        ["exactly 5 chars", "Abcde"],
        ["exactly 100 chars", "A".repeat(100)],
        ["mid-range", "Monday Lower Body"],
        ["trims surrounding whitespace before measuring", "  Abcde  "],
    ])("accepts %s", (_label, name) => {
        const result = validateSessionName(name);

        expect(result.valid).toBe(true);
    });

    it.each([
        ["missing", undefined],
        ["null", null],
        ["empty", ""],
        ["whitespace-only", "    "],
        ["4 chars", "Abcd"],
        ["101 chars", "A".repeat(101)],
    ])("rejects %s", (_label, name) => {
        const result = validateSessionName(name);

        expect(result.valid).toBe(false);
        expect(result.error).toBeTypeOf("string");
    });
});
