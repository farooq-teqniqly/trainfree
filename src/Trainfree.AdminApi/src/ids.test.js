import { describe, expect, it } from "vitest";
import {
    generateId,
    generatePhaseId,
    generateProgramId,
    generateSessionId,
    isValidId,
    isValidPhaseId,
    isValidProgramId,
    isValidSessionId,
} from "./ids.js";

describe("generateId", () => {
    it("produces a prefixed id with a 6-character Crockford base32 body", () => {
        const id = generateId("XYZ-");

        expect(id).toMatch(/^XYZ-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/);
    });

    it("produces effectively unique ids across calls", () => {
        const ids = new Set(Array.from({ length: 50 }, () => generateId("XYZ-")));

        expect(ids.size).toBe(50);
    });
});

describe("isValidId", () => {
    it.each([["XYZ-7K2QXM"], ["XYZ-234567"], ["XYZ-ABCDEF"]])(
        "accepts a well-formed id %s",
        (value) => {
            expect(isValidId(value, "XYZ-")).toBe(true);
        },
    );

    it.each([
        [null],
        [undefined],
        [""],
        ["XYZ-7K2QX"],
        ["XYZ-7K2QXMM"],
        ["XYZ-7K2Q0M"],
        ["XYZ-7K2Q1M"],
        ["XYZ-7K2QOM"],
        ["XYZ-7K2QIM"],
        ["XYZ-7K2QLM"],
        ["xyz-7K2QXM"],
        ["ABC-7K2QXM"],
        ["XYZ7K2QXM"],
    ])("rejects an ill-formed id %s", (value) => {
        expect(isValidId(value, "XYZ-")).toBe(false);
    });
});

describe("generateProgramId", () => {
    it("produces a PRG- prefixed id with a 6-character Crockford base32 body", () => {
        const id = generateProgramId();

        expect(id).toMatch(/^PRG-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/);
    });

    it("produces effectively unique ids across calls", () => {
        const ids = new Set(Array.from({ length: 50 }, () => generateProgramId()));

        expect(ids.size).toBe(50);
    });
});

describe("isValidProgramId", () => {
    it.each([["PRG-7K2QXM"], ["PRG-234567"], ["PRG-ABCDEF"]])(
        "accepts a well-formed id %s",
        (value) => {
            expect(isValidProgramId(value)).toBe(true);
        },
    );

    it.each([
        [null],
        [undefined],
        [""],
        ["PRG-7K2QX"],
        ["PRG-7K2QXMM"],
        ["PRG-7K2Q0M"],
        ["PRG-7K2Q1M"],
        ["PRG-7K2QOM"],
        ["PRG-7K2QIM"],
        ["PRG-7K2QLM"],
        ["prg-7K2QXM"],
        ["XYZ-7K2QXM"],
        ["PRG7K2QXM"],
    ])("rejects an ill-formed id %s", (value) => {
        expect(isValidProgramId(value)).toBe(false);
    });
});

describe("generateSessionId", () => {
    it("produces a SNN- prefixed id with a 6-character Crockford base32 body", () => {
        const id = generateSessionId();

        expect(id).toMatch(/^SNN-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/);
    });

    it("produces effectively unique ids across calls", () => {
        const ids = new Set(Array.from({ length: 50 }, () => generateSessionId()));

        expect(ids.size).toBe(50);
    });
});

describe("isValidSessionId", () => {
    it.each([["SNN-7K2QXM"], ["SNN-234567"], ["SNN-ABCDEF"]])(
        "accepts a well-formed id %s",
        (value) => {
            expect(isValidSessionId(value)).toBe(true);
        },
    );

    it.each([
        [null],
        [undefined],
        [""],
        ["SNN-7K2QX"],
        ["SNN-7K2QXMM"],
        ["SNN-7K2Q0M"],
        ["SNN-7K2Q1M"],
        ["SNN-7K2QOM"],
        ["SNN-7K2QIM"],
        ["SNN-7K2QLM"],
        ["snn-7K2QXM"],
        ["PRG-7K2QXM"],
        ["SNN7K2QXM"],
    ])("rejects an ill-formed id %s", (value) => {
        expect(isValidSessionId(value)).toBe(false);
    });
});

describe("generatePhaseId", () => {
    it("produces a PHS- prefixed id with a 6-character Crockford base32 body", () => {
        const id = generatePhaseId();

        expect(id).toMatch(/^PHS-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/);
    });

    it("produces effectively unique ids across calls", () => {
        const ids = new Set(Array.from({ length: 50 }, () => generatePhaseId()));

        expect(ids.size).toBe(50);
    });
});

describe("isValidPhaseId", () => {
    it.each([["PHS-7K2QXM"], ["PHS-234567"], ["PHS-ABCDEF"]])(
        "accepts a well-formed id %s",
        (value) => {
            expect(isValidPhaseId(value)).toBe(true);
        },
    );

    it.each([
        [null],
        [undefined],
        [""],
        ["PHS-7K2QX"],
        ["PHS-7K2QXMM"],
        ["PHS-7K2Q0M"],
        ["PHS-7K2Q1M"],
        ["PHS-7K2QOM"],
        ["PHS-7K2QIM"],
        ["PHS-7K2QLM"],
        ["phs-7K2QXM"],
        ["PRG-7K2QXM"],
        ["PHS7K2QXM"],
    ])("rejects an ill-formed id %s", (value) => {
        expect(isValidPhaseId(value)).toBe(false);
    });
});
