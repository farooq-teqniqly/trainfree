export class DuplicateNameError extends Error {
    constructor(name, entityLabel = "program") {
        super(`A ${entityLabel} named "${name}" already exists.`);
        this.name = "DuplicateNameError";
    }
}

export class PhaseInUseError extends Error {
    constructor(id) {
        super(`Phase "${id}" is referenced by at least one session and cannot be deleted.`);
        this.name = "PhaseInUseError";
    }
}

export class SessionPhaseInvalidPhaseError extends Error {
    constructor() {
        super("phaseId is required and must reference an existing phase");
        this.name = "SessionPhaseInvalidPhaseError";
    }
}

export class SessionNotFoundError extends Error {
    constructor(id) {
        super(`Session "${id}" was not found.`);
        this.name = "SessionNotFoundError";
    }
}

export class ExerciseInUseError extends Error {
    constructor(id) {
        super(
            `Exercise "${id}" is referenced by at least one program exercise and cannot be deleted.`,
        );
        this.name = "ExerciseInUseError";
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

// Unlike a UNIQUE violation, D1/SQLite's FOREIGN KEY violation message never names the
// offending table or column -- it is the fixed string "FOREIGN KEY constraint failed:
// SQLITE_CONSTRAINT" regardless of which foreign key fired. So this detector, unlike
// uniqueConstraintColumns, takes no table/column argument; a caller whose statement can
// violate more than one foreign key (e.g. session_phases' session_id and phase_id) must
// re-check each referenced row itself afterward to tell which one actually failed --
// see createSessionPhase in session-phases.js.
export function isForeignKeyViolation(err) {
    if (!(err instanceof Error)) {
        return false;
    }

    return /FOREIGN KEY constraint failed/i.test(err.message);
}
