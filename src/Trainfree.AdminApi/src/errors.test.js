import { describe, expect, it } from "vitest";
import { uniqueConstraintColumns } from "./errors.js";

describe("uniqueConstraintColumns", () => {
    it("returns an empty array when err is not an Error instance", () => {
        expect(uniqueConstraintColumns("not an error", "programs")).toEqual([]);
    });

    it("extracts every matching column for the given table", () => {
        const err = new Error(
            "D1_ERROR: UNIQUE constraint failed: programs.name: SQLITE_CONSTRAINT",
        );

        expect(uniqueConstraintColumns(err, "programs")).toEqual(["name"]);
    });

    it("does not match a different table's column", () => {
        const err = new Error(
            "D1_ERROR: UNIQUE constraint failed: sessions.name: SQLITE_CONSTRAINT",
        );

        expect(uniqueConstraintColumns(err, "programs")).toEqual([]);
    });

    it("treats a table name containing an unescaped regex metacharacter literally", () => {
        // An unmatched "[" is invalid regex syntax; unescaped, this table name would
        // make `new RegExp(...)` throw instead of returning no matches.
        const err = new Error(
            "D1_ERROR: UNIQUE constraint failed: sessions.name: SQLITE_CONSTRAINT",
        );

        expect(() => uniqueConstraintColumns(err, "sessions[")).not.toThrow();
        expect(uniqueConstraintColumns(err, "sessions[")).toEqual([]);
    });
});
