import { generateSessionId } from "./ids.js";
import { DuplicateNameError, uniqueConstraintColumns } from "./errors.js";

const SELECT_COLUMNS =
    "session_id as id, program_id as programId, name, created_at as createdAt, updated_at as updatedAt";

// generateSessionId draws from a ~30^6 (~7e8, ~29-bit) space, so a collision is
// unlikely; this bound only guards against pathological bad luck, not a real retry loop.
const MAX_ID_GENERATION_ATTEMPTS = 5;

export async function programExists(db, programId) {
    const row = await db
        .prepare("SELECT 1 FROM programs WHERE program_id = ?")
        .bind(programId)
        .first();
    return row !== null;
}

export async function listSessions(db, programId) {
    const { results } = await db
        .prepare(
            // ORDER BY must qualify id as sessions.id: SELECT_COLUMNS aliases session_id
            // as id, and an unqualified "id" in ORDER BY resolves to that output alias
            // rather than the table's autoincrement id column.
            `SELECT ${SELECT_COLUMNS} FROM sessions WHERE program_id = ? ORDER BY created_at ASC, sessions.id ASC`,
        )
        .bind(programId)
        .all();
    return results;
}

export async function createSession(db, programId, name) {
    const now = new Date().toISOString();

    for (let attempt = 1; attempt <= MAX_ID_GENERATION_ATTEMPTS; attempt++) {
        const id = generateSessionId();

        try {
            await db
                .prepare(
                    "INSERT INTO sessions (session_id, program_id, name, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
                )
                .bind(id, programId, name, now, now)
                .run();
            return { id, programId, name, createdAt: now, updatedAt: now };
        } catch (err) {
            const columns = uniqueConstraintColumns(err, "sessions");
            if (columns.includes("name")) {
                throw new DuplicateNameError(name, "session");
            }
            if (columns.includes("session_id") && attempt < MAX_ID_GENERATION_ATTEMPTS) {
                continue;
            }
            throw err;
        }
    }

    // Unreachable: every loop iteration above either returns or throws. Kept so the
    // function has an explicit terminal path rather than an implicit `return undefined`
    // control-flow analysis can't rule out from the loop alone.
    throw new Error("Failed to generate a unique session id after multiple attempts.");
}

export async function renameSession(db, programId, id, name) {
    const now = new Date().toISOString();

    let result;
    try {
        result = await db
            .prepare(
                "UPDATE sessions SET name = ?, updated_at = ? WHERE session_id = ? AND program_id = ?",
            )
            .bind(name, now, id, programId)
            .run();
    } catch (err) {
        if (uniqueConstraintColumns(err, "sessions").includes("name")) {
            throw new DuplicateNameError(name, "session");
        }
        throw err;
    }

    if (result.meta.changes === 0) {
        return null;
    }

    return db
        .prepare(`SELECT ${SELECT_COLUMNS} FROM sessions WHERE session_id = ?`)
        .bind(id)
        .first();
}

export async function deleteSession(db, programId, id) {
    const result = await db
        .prepare("DELETE FROM sessions WHERE session_id = ? AND program_id = ?")
        .bind(id, programId)
        .run();
    return result.meta.changes > 0;
}
