import { shapeProgramExerciseRow } from "./program-exercises.js";

const PROGRAMS_QUERY =
    "SELECT program_id as id, name, created_at as createdAt, updated_at as updatedAt " +
    "FROM programs ORDER BY created_at ASC, programs.id ASC";

const SESSIONS_QUERY =
    "SELECT session_id as id, program_id as programId, name, created_at as createdAt, updated_at as updatedAt " +
    "FROM sessions ORDER BY created_at ASC, sessions.id ASC";

const SESSION_PHASES_QUERY =
    "SELECT session_phase_id as id, session_id as sessionId, phase_id as phaseId, created_at as createdAt " +
    "FROM session_phases ORDER BY created_at ASC, session_phases.id ASC";

const PROGRAM_EXERCISES_QUERY =
    "SELECT program_exercise_id as id, session_phase_id as sessionPhaseId, exercise_id as exerciseId, " +
    "type, reps, duration_seconds as durationSeconds, weight, sets, rest_seconds as restSeconds, " +
    "side, created_at as createdAt, updated_at as updatedAt " +
    "FROM program_exercises ORDER BY created_at ASC, program_exercises.id ASC";

// Groups rows by a key column into a Map, so a parent with no matching children can be
// given [] via `?? []` rather than being silently omitted from the tree -- unlike an
// INNER JOIN, which would drop a program/session/phase that has no children yet, a
// normal, expected state while a program is still being built out in the Admin UI.
function groupBy(rows, key) {
    const groups = new Map();
    for (const row of rows) {
        const groupKey = row[key];
        const group = groups.get(groupKey);
        if (group) {
            group.push(row);
        } else {
            groups.set(groupKey, [row]);
        }
    }
    return groups;
}

export async function listProgramsTree(db) {
    const [programsResult, sessionsResult, sessionPhasesResult, programExercisesResult] =
        await db.batch([
            db.prepare(PROGRAMS_QUERY),
            db.prepare(SESSIONS_QUERY),
            db.prepare(SESSION_PHASES_QUERY),
            db.prepare(PROGRAM_EXERCISES_QUERY),
        ]);

    const exercisesByPhaseId = groupBy(
        programExercisesResult.results.map(shapeProgramExerciseRow),
        "sessionPhaseId",
    );
    const phasesBySessionId = groupBy(sessionPhasesResult.results, "sessionId");
    const sessionsByProgramId = groupBy(sessionsResult.results, "programId");

    return programsResult.results.map((program) => ({
        ...program,
        sessions: (sessionsByProgramId.get(program.id) ?? []).map((session) => ({
            ...session,
            phases: (phasesBySessionId.get(session.id) ?? []).map((phase) => ({
                ...phase,
                exercises: exercisesByPhaseId.get(phase.id) ?? [],
            })),
        })),
    }));
}
