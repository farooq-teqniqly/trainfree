import { generateProgramId } from "./ids.js";
import { DuplicateNameError, uniqueConstraintColumns } from "./errors.js";

const SELECT_COLUMNS =
    "program_id as id, name, created_at as createdAt, updated_at as updatedAt";

// generateProgramId draws from a ~150-bit space (31^6), so a collision is exceedingly
// unlikely; this bound only guards against pathological bad luck, not a real retry loop.
const MAX_ID_GENERATION_ATTEMPTS = 5;

export async function listPrograms(db) {
    const { results } = await db
        .prepare(`SELECT ${SELECT_COLUMNS} FROM programs ORDER BY created_at ASC`)
        .all();
    return results;
}

export async function createProgram(db, name) {
    const now = new Date().toISOString();

    for (let attempt = 1; attempt <= MAX_ID_GENERATION_ATTEMPTS; attempt++) {
        const id = generateProgramId();

        try {
            await db
                .prepare(
                    "INSERT INTO programs (program_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
                )
                .bind(id, name, now, now)
                .run();
            return { id, name, createdAt: now, updatedAt: now };
        } catch (err) {
            const columns = uniqueConstraintColumns(err);
            if (columns.includes("name")) {
                throw new DuplicateNameError(name);
            }
            if (columns.includes("program_id") && attempt < MAX_ID_GENERATION_ATTEMPTS) {
                continue;
            }
            throw err;
        }
    }
}

export async function renameProgram(db, id, name) {
    const now = new Date().toISOString();

    let result;
    try {
        result = await db
            .prepare("UPDATE programs SET name = ?, updated_at = ? WHERE program_id = ?")
            .bind(name, now, id)
            .run();
    } catch (err) {
        if (uniqueConstraintColumns(err).includes("name")) {
            throw new DuplicateNameError(name);
        }
        throw err;
    }

    if (result.meta.changes === 0) {
        return null;
    }

    return db
        .prepare(`SELECT ${SELECT_COLUMNS} FROM programs WHERE program_id = ?`)
        .bind(id)
        .first();
}

export async function deleteProgram(db, id) {
    const result = await db.prepare("DELETE FROM programs WHERE program_id = ?").bind(id).run();
    return result.meta.changes > 0;
}
