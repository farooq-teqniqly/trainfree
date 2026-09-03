import { generatePhaseId } from "./ids.js";
import { DuplicateNameError, uniqueConstraintColumns } from "./errors.js";

const SELECT_COLUMNS =
    "phase_id as id, name, created_at as createdAt, updated_at as updatedAt";

// generatePhaseId draws from a ~30^6 (~7e8, ~29-bit) space, so a collision is
// unlikely; this bound only guards against pathological bad luck, not a real retry loop.
const MAX_ID_GENERATION_ATTEMPTS = 5;

export async function listPhases(db) {
    const { results } = await db
        .prepare(`SELECT ${SELECT_COLUMNS} FROM phases ORDER BY created_at ASC`)
        .all();
    return results;
}

export async function createPhase(db, name) {
    const now = new Date().toISOString();

    for (let attempt = 1; attempt <= MAX_ID_GENERATION_ATTEMPTS; attempt++) {
        const id = generatePhaseId();

        try {
            await db
                .prepare(
                    "INSERT INTO phases (phase_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
                )
                .bind(id, name, now, now)
                .run();
            return { id, name, createdAt: now, updatedAt: now };
        } catch (err) {
            const columns = uniqueConstraintColumns(err, "phases");
            if (columns.includes("name")) {
                throw new DuplicateNameError(name, "phase");
            }
            if (columns.includes("phase_id") && attempt < MAX_ID_GENERATION_ATTEMPTS) {
                continue;
            }
            throw err;
        }
    }

    // Unreachable: every loop iteration above either returns or throws. Kept so the
    // function has an explicit terminal path rather than an implicit `return undefined`
    // control-flow analysis can't rule out from the loop alone.
    throw new Error("Failed to generate a unique phase id after multiple attempts.");
}

export async function renamePhase(db, id, name) {
    const now = new Date().toISOString();

    let result;
    try {
        result = await db
            .prepare("UPDATE phases SET name = ?, updated_at = ? WHERE phase_id = ?")
            .bind(name, now, id)
            .run();
    } catch (err) {
        if (uniqueConstraintColumns(err, "phases").includes("name")) {
            throw new DuplicateNameError(name, "phase");
        }
        throw err;
    }

    if (result.meta.changes === 0) {
        return null;
    }

    return db
        .prepare(`SELECT ${SELECT_COLUMNS} FROM phases WHERE phase_id = ?`)
        .bind(id)
        .first();
}

export async function deletePhase(db, id) {
    const result = await db
        .prepare("DELETE FROM phases WHERE phase_id = ?")
        .bind(id)
        .run();
    return result.meta.changes > 0;
}
