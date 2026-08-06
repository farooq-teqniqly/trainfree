import { generateProgramId } from "./ids.js";

const SELECT_COLUMNS =
    "program_id as id, name, created_at as createdAt, updated_at as updatedAt";

export async function listPrograms(db) {
    const { results } = await db
        .prepare(`SELECT ${SELECT_COLUMNS} FROM programs ORDER BY created_at ASC`)
        .all();
    return results;
}

export async function createProgram(db, name) {
    const id = generateProgramId();
    const now = new Date().toISOString();

    await db
        .prepare("INSERT INTO programs (program_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)")
        .bind(id, name, now, now)
        .run();

    return { id, name, createdAt: now, updatedAt: now };
}

export async function renameProgram(db, id, name) {
    const now = new Date().toISOString();

    const result = await db
        .prepare("UPDATE programs SET name = ?, updated_at = ? WHERE program_id = ?")
        .bind(name, now, id)
        .run();

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
