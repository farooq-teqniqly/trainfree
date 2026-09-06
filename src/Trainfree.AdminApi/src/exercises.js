import { generateExerciseId } from "./ids.js";
import { DuplicateNameError, uniqueConstraintColumns } from "./errors.js";

const SELECT_COLUMNS =
    "exercise_id as id, name, created_at as createdAt, updated_at as updatedAt";

// generateExerciseId draws from a ~30^6 (~7e8, ~29-bit) space, so a collision is
// unlikely; this bound only guards against pathological bad luck, not a real retry loop.
const MAX_ID_GENERATION_ATTEMPTS = 5;

// Exported so tests can assert on the literal tiebreak clause without mocking the D1
// binding (CLAUDE-baseline.md forbids mocking Worker/D1 test dependencies).
export const LIST_EXERCISES_QUERY = `SELECT ${SELECT_COLUMNS} FROM exercises ORDER BY created_at ASC, exercises.id ASC`;

export async function listExercises(db) {
    const { results } = await db.prepare(LIST_EXERCISES_QUERY).all();
    return results;
}

export async function createExercise(db, name) {
    const now = new Date().toISOString();

    for (let attempt = 1; attempt <= MAX_ID_GENERATION_ATTEMPTS; attempt++) {
        const id = generateExerciseId();

        try {
            await db
                .prepare(
                    "INSERT INTO exercises (exercise_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
                )
                .bind(id, name, now, now)
                .run();
            return { id, name, createdAt: now, updatedAt: now };
        } catch (err) {
            const columns = uniqueConstraintColumns(err, "exercises");
            if (columns.includes("name")) {
                throw new DuplicateNameError(name, "exercise");
            }
            if (columns.includes("exercise_id") && attempt < MAX_ID_GENERATION_ATTEMPTS) {
                continue;
            }
            throw err;
        }
    }

    // Unreachable: every loop iteration above either returns or throws. Kept so the
    // function has an explicit terminal path rather than an implicit `return undefined`
    // control-flow analysis can't rule out from the loop alone.
    throw new Error("Failed to generate a unique exercise id after multiple attempts.");
}

export async function renameExercise(db, id, name) {
    const now = new Date().toISOString();

    let result;
    try {
        result = await db
            .prepare("UPDATE exercises SET name = ?, updated_at = ? WHERE exercise_id = ?")
            .bind(name, now, id)
            .run();
    } catch (err) {
        if (uniqueConstraintColumns(err, "exercises").includes("name")) {
            throw new DuplicateNameError(name, "exercise");
        }
        throw err;
    }

    if (result.meta.changes === 0) {
        return null;
    }

    return db
        .prepare(`SELECT ${SELECT_COLUMNS} FROM exercises WHERE exercise_id = ?`)
        .bind(id)
        .first();
}

export async function deleteExercise(db, id) {
    const result = await db
        .prepare("DELETE FROM exercises WHERE exercise_id = ?")
        .bind(id)
        .run();
    return result.meta.changes > 0;
}
