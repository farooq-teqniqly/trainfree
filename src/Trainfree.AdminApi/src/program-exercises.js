import { generateProgramExerciseId } from "./ids.js";
import { uniqueConstraintColumns } from "./errors.js";

const SELECT_COLUMNS =
    "program_exercise_id as id, session_phase_id as sessionPhaseId, exercise_id as exerciseId, " +
    "type, reps, duration_seconds as durationSeconds, weight, sets, rest_seconds as restSeconds, " +
    "side, created_at as createdAt, updated_at as updatedAt";

// generateProgramExerciseId draws from a ~30^6 (~7e8, ~29-bit) space, so a collision is
// unlikely; this bound only guards against pathological bad luck, not a real retry loop.
const MAX_ID_GENERATION_ATTEMPTS = 5;

const UPDATE_COLUMNS_BY_FIELD = {
    reps: "reps",
    durationSeconds: "duration_seconds",
    sets: "sets",
    restSeconds: "rest_seconds",
    weight: "weight",
    side: "side",
};

// A RepsProgramExercise row carries no durationSeconds property, and a
// TimedProgramExercise row carries no reps property -- the D1 column for the other type's
// count is always NULL, and this strips it rather than exposing it as `null` (which would
// read as "type has this field but it's unset" instead of "type does not have this field").
function shapeProgramExerciseRow(row) {
    const shaped = { ...row };
    if (shaped.type === "Reps") {
        delete shaped.durationSeconds;
    } else {
        delete shaped.reps;
    }
    return shaped;
}

export async function programExerciseSessionPhaseExists(db, sessionId, sessionPhaseId) {
    const row = await db
        .prepare("SELECT 1 FROM session_phases WHERE session_phase_id = ? AND session_id = ?")
        .bind(sessionPhaseId, sessionId)
        .first();
    return row !== null;
}

export async function listProgramExercises(db, sessionPhaseId) {
    const { results } = await db
        .prepare(
            // ORDER BY must qualify id as program_exercises.id: SELECT_COLUMNS aliases
            // program_exercise_id as id, and an unqualified "id" in ORDER BY resolves to
            // that output alias rather than the table's autoincrement id column.
            `SELECT ${SELECT_COLUMNS} FROM program_exercises WHERE session_phase_id = ? ORDER BY created_at ASC, program_exercises.id ASC`,
        )
        .bind(sessionPhaseId)
        .all();
    return results.map(shapeProgramExerciseRow);
}

export async function createProgramExercise(db, sessionPhaseId, values) {
    const now = new Date().toISOString();
    const reps = values.type === "Reps" ? values.reps : null;
    const durationSeconds = values.type === "Timed" ? values.durationSeconds : null;

    for (let attempt = 1; attempt <= MAX_ID_GENERATION_ATTEMPTS; attempt++) {
        const id = generateProgramExerciseId();

        try {
            await db
                .prepare(
                    "INSERT INTO program_exercises (program_exercise_id, session_phase_id, exercise_id, type, reps, duration_seconds, weight, sets, rest_seconds, side, created_at, updated_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                )
                .bind(
                    id,
                    sessionPhaseId,
                    values.exerciseId,
                    values.type,
                    reps,
                    durationSeconds,
                    values.weight,
                    values.sets,
                    values.restSeconds,
                    values.side,
                    now,
                    now,
                )
                .run();
            return shapeProgramExerciseRow({
                id,
                sessionPhaseId,
                exerciseId: values.exerciseId,
                type: values.type,
                reps,
                durationSeconds,
                weight: values.weight,
                sets: values.sets,
                restSeconds: values.restSeconds,
                side: values.side,
                createdAt: now,
                updatedAt: now,
            });
        } catch (err) {
            const columns = uniqueConstraintColumns(err, "program_exercises");
            if (
                columns.includes("program_exercise_id") &&
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
    throw new Error("Failed to generate a unique program exercise id after multiple attempts.");
}

export async function updateProgramExercise(db, sessionPhaseId, id, updates) {
    const now = new Date().toISOString();
    const setClauses = ["updated_at = ?"];
    const bindings = [now];

    for (const [field, value] of Object.entries(updates)) {
        setClauses.push(`${UPDATE_COLUMNS_BY_FIELD[field]} = ?`);
        bindings.push(value);
    }

    bindings.push(id, sessionPhaseId);

    const result = await db
        .prepare(
            `UPDATE program_exercises SET ${setClauses.join(", ")} WHERE program_exercise_id = ? AND session_phase_id = ?`,
        )
        .bind(...bindings)
        .run();

    if (result.meta.changes === 0) {
        return null;
    }

    const row = await db
        .prepare(`SELECT ${SELECT_COLUMNS} FROM program_exercises WHERE program_exercise_id = ?`)
        .bind(id)
        .first();
    return shapeProgramExerciseRow(row);
}

export async function deleteProgramExercise(db, sessionPhaseId, id) {
    const result = await db
        .prepare(
            "DELETE FROM program_exercises WHERE program_exercise_id = ? AND session_phase_id = ?",
        )
        .bind(id, sessionPhaseId)
        .run();
    return result.meta.changes > 0;
}
