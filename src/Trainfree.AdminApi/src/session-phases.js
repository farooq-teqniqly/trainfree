import { generateSessionPhaseId } from "./ids.js";
import {
    SessionNotFoundError,
    SessionPhaseInvalidPhaseError,
    isForeignKeyViolation,
    uniqueConstraintColumns,
} from "./errors.js";

const SELECT_COLUMNS =
    "session_phase_id as id, session_id as sessionId, phase_id as phaseId, created_at as createdAt";

// generateSessionPhaseId draws from a ~30^6 (~7e8, ~29-bit) space, so a collision is
// unlikely; this bound only guards against pathological bad luck, not a real retry loop.
const MAX_ID_GENERATION_ATTEMPTS = 5;

export async function sessionExists(db, programId, sessionId) {
    const row = await db
        .prepare("SELECT 1 FROM sessions WHERE session_id = ? AND program_id = ?")
        .bind(sessionId, programId)
        .first();
    return row !== null;
}

// Unscoped by programId, unlike sessionExists: only used to disambiguate which of
// session_phases' two foreign keys fired on an INSERT failure (see createSessionPhase
// below), where the caller has already confirmed the session under its program before
// reaching this point -- re-checking programId here would just repeat that check.
async function sessionRowExists(db, sessionId) {
    const row = await db.prepare("SELECT 1 FROM sessions WHERE session_id = ?").bind(sessionId).first();
    return row !== null;
}

export async function listSessionPhases(db, sessionId) {
    const { results } = await db
        .prepare(
            // ORDER BY must qualify id as session_phases.id: SELECT_COLUMNS aliases
            // session_phase_id as id, and an unqualified "id" in ORDER BY resolves to
            // that output alias rather than the table's autoincrement id column.
            `SELECT ${SELECT_COLUMNS} FROM session_phases WHERE session_id = ? ORDER BY created_at ASC, session_phases.id ASC`,
        )
        .bind(sessionId)
        .all();
    return results;
}

export async function createSessionPhase(db, sessionId, phaseId) {
    const now = new Date().toISOString();

    for (let attempt = 1; attempt <= MAX_ID_GENERATION_ATTEMPTS; attempt++) {
        const id = generateSessionPhaseId();

        try {
            await db
                .prepare(
                    "INSERT INTO session_phases (session_phase_id, session_id, phase_id, created_at) VALUES (?, ?, ?, ?)",
                )
                .bind(id, sessionId, phaseId, now)
                .run();
            return { id, sessionId, phaseId, createdAt: now };
        } catch (err) {
            const columns = uniqueConstraintColumns(err, "session_phases");
            if (
                columns.includes("session_phase_id") &&
                attempt < MAX_ID_GENERATION_ATTEMPTS
            ) {
                continue;
            }
            // This INSERT references two foreign keys (session_id, phase_id), and D1's
            // FK violation message doesn't say which one fired -- so a concurrent
            // DELETE of either the session or the phase between the caller's own
            // existence checks and this INSERT (issue #89) surfaces the same generic
            // error. Re-check the session specifically to tell the two apart: it's
            // still there, so the failure must be the caller's phaseId going stale.
            if (isForeignKeyViolation(err)) {
                if (!(await sessionRowExists(db, sessionId))) {
                    throw new SessionNotFoundError(sessionId);
                }
                throw new SessionPhaseInvalidPhaseError();
            }
            throw err;
        }
    }

    // Unreachable: every loop iteration above either returns or throws. Kept so the
    // function has an explicit terminal path rather than an implicit `return undefined`
    // control-flow analysis can't rule out from the loop alone.
    throw new Error("Failed to generate a unique session phase id after multiple attempts.");
}

export async function deleteSessionPhase(db, sessionId, id) {
    const result = await db
        .prepare("DELETE FROM session_phases WHERE session_phase_id = ? AND session_id = ?")
        .bind(id, sessionId)
        .run();
    return result.meta.changes > 0;
}
