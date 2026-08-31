export class DuplicateNameError extends Error {
    constructor(name, entityLabel = "program") {
        super(`A ${entityLabel} named "${name}" already exists.`);
        this.name = "DuplicateNameError";
    }
}

// SQLite's UNIQUE-violation text names every column of the constraint that fired as
// "<table>.<column>", e.g. "D1_ERROR: UNIQUE constraint failed: programs.name:
// SQLITE_CONSTRAINT". A table can have more than one such column -- e.g. `programs`
// has both `program_id` (generated-ID collision, not user-facing) and `name` (a real
// duplicate-name conflict). Matching "<table>.<word>" directly, rather than parsing
// the whole trailing clause, is resilient to whatever D1/SQLite appends after the
// column list (driver-specific error codes, multiple columns, etc.).
function escapeRegExp(value) {
    return value.replace(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`);
}

export function uniqueConstraintColumns(err, table) {
    if (!(err instanceof Error)) {
        return [];
    }

    const matches = err.message.matchAll(
        new RegExp(String.raw`\b${escapeRegExp(table)}\.(\w+)`, "gi"),
    );
    return [...matches].map((match) => match[1]);
}
