import { generateExerciseId } from "./ids.js";
import { DuplicateNameError, ExerciseInUseError, uniqueConstraintColumns } from "./errors.js";

const SELECT_COLUMNS =
    "exercise_id as id, name, image_key as imageKey, created_at as createdAt, updated_at as updatedAt";

const IMAGE_KEY_PREFIX = "exercises";

// The R2 key never leaves the Worker. The key's last segment is a fresh random UUID per
// upload, so using it as the `v` query param makes the URL change on every replace (which
// keeps the image route's immutable Cache-Control safe) without exposing the key itself.
function toExercise({ id, name, imageKey, createdAt, updatedAt }) {
    const version = imageKey?.split("/").pop();
    const imageUrl = version ? `/api/exercises/${id}/image?v=${version}` : null;
    return { id, name, imageUrl, createdAt, updatedAt };
}

// generateExerciseId draws from a ~30^6 (~7e8, ~29-bit) space, so a collision is
// unlikely; this bound only guards against pathological bad luck, not a real retry loop.
const MAX_ID_GENERATION_ATTEMPTS = 5;

// Exported so tests can assert on the literal tiebreak clause without mocking the D1
// binding (CLAUDE-baseline.md forbids mocking Worker/D1 test dependencies).
export const LIST_EXERCISES_QUERY = `SELECT ${SELECT_COLUMNS} FROM exercises ORDER BY created_at ASC, exercises.id ASC`;

export async function exerciseExists(db, id) {
    const row = await db
        .prepare("SELECT 1 FROM exercises WHERE exercise_id = ?")
        .bind(id)
        .first();
    return row !== null;
}

export async function listExercises(db) {
    const { results } = await db.prepare(LIST_EXERCISES_QUERY).all();
    return results.map(toExercise);
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
            return { id, name, imageUrl: null, createdAt: now, updatedAt: now };
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

    return getExercise(db, id);
}

async function getExercise(db, id) {
    const row = await db
        .prepare(`SELECT ${SELECT_COLUMNS} FROM exercises WHERE exercise_id = ?`)
        .bind(id)
        .first();
    return row ? toExercise(row) : null;
}

async function getImageKey(db, id) {
    const row = await db
        .prepare("SELECT image_key as imageKey FROM exercises WHERE exercise_id = ?")
        .bind(id)
        .first();
    return row?.imageKey ?? null;
}

// Stores `bytes` under a new random key, points the row at it, then deletes the previous
// object. Ordering matters: a failure before the row update leaves the old image
// referenced and intact; a failure after it leaves only an unreferenced old object,
// never a row pointing at nothing. Returns null when the exercise does not exist.
export async function setExerciseImage(db, bucket, id, bytes, contentType) {
    if (!(await exerciseExists(db, id))) {
        return null;
    }

    const previousKey = await getImageKey(db, id);
    const newKey = `${IMAGE_KEY_PREFIX}/${id}/${crypto.randomUUID()}`;
    await bucket.put(newKey, bytes, { httpMetadata: { contentType } });

    const result = await db
        .prepare("UPDATE exercises SET image_key = ?, updated_at = ? WHERE exercise_id = ?")
        .bind(newKey, new Date().toISOString(), id)
        .run();
    if (result.meta.changes === 0) {
        await bucket.delete(newKey);
        return null;
    }

    if (previousKey) {
        await bucket.delete(previousKey);
    }
    return getExercise(db, id);
}

// Returns { body, contentType } for the exercise's image, or null when the exercise does
// not exist, has no image, or its object is missing from the bucket.
export async function getExerciseImage(db, bucket, id) {
    const key = await getImageKey(db, id);
    if (!key) {
        return null;
    }

    const object = await bucket.get(key);
    if (!object) {
        return null;
    }
    return { body: object.body, contentType: object.httpMetadata?.contentType };
}

// Returns false when the exercise does not exist or has no image.
export async function deleteExerciseImage(db, bucket, id) {
    const key = await getImageKey(db, id);
    if (!key) {
        return false;
    }

    await db
        .prepare("UPDATE exercises SET image_key = NULL, updated_at = ? WHERE exercise_id = ?")
        .bind(new Date().toISOString(), id)
        .run();
    await bucket.delete(key);
    return true;
}

export async function deleteExercise(db, id, bucket) {
    const inUse = await db
        .prepare("SELECT 1 FROM program_exercises WHERE exercise_id = ?")
        .bind(id)
        .first();
    if (inUse !== null) {
        throw new ExerciseInUseError(id);
    }

    const imageKey = await getImageKey(db, id);
    const result = await db
        .prepare("DELETE FROM exercises WHERE exercise_id = ?")
        .bind(id)
        .run();
    if (result.meta.changes === 0) {
        return false;
    }

    if (imageKey && bucket) {
        await bucket.delete(imageKey);
    }
    return true;
}
