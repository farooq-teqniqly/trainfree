export class DuplicateNameError extends Error {
    constructor(name) {
        super(`A program named "${name}" already exists.`);
        this.name = "DuplicateNameError";
    }
}

// SQLite's UNIQUE-violation text names every column of the constraint that fired as
// "<table>.<column>", e.g. "D1_ERROR: UNIQUE constraint failed: programs.name:
// SQLITE_CONSTRAINT". `programs` has two such columns: `program_id` (generated-ID
// collision, not user-facing) and `name` (a real duplicate-name conflict). Matching
// "programs.<word>" directly, rather than parsing the whole trailing clause, is
// resilient to whatever D1/SQLite appends after the column list (driver-specific error
// codes, multiple columns, etc.).
export function uniqueConstraintColumns(err) {
    if (!(err instanceof Error)) {
        return [];
    }

    const matches = err.message.matchAll(/\bprograms\.(\w+)/gi);
    return [...matches].map((match) => match[1]);
}
