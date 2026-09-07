import { generateSessionPhaseId } from "./ids.js";
import { uniqueConstraintColumns } from "./errors.js";

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
