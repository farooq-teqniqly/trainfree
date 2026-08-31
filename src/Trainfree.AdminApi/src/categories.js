import { generateCategoryId } from "./ids.js";
import { DuplicateNameError, uniqueConstraintColumns } from "./errors.js";

const SELECT_COLUMNS =
    "category_id as id, name, created_at as createdAt, updated_at as updatedAt";

// generateCategoryId draws from a ~30^6 (~7e8, ~29-bit) space, so a collision is
// unlikely; this bound only guards against pathological bad luck, not a real retry loop.
const MAX_ID_GENERATION_ATTEMPTS = 5;

export async function listCategories(db) {
    const { results } = await db
        .prepare(`SELECT ${SELECT_COLUMNS} FROM categories ORDER BY created_at ASC`)
        .all();
    return results;
}

export async function createCategory(db, name) {
    const now = new Date().toISOString();

    for (let attempt = 1; attempt <= MAX_ID_GENERATION_ATTEMPTS; attempt++) {
        const id = generateCategoryId();

        try {
            await db
                .prepare(
                    "INSERT INTO categories (category_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
                )
                .bind(id, name, now, now)
                .run();
            return { id, name, createdAt: now, updatedAt: now };
        } catch (err) {
            const columns = uniqueConstraintColumns(err, "categories");
            if (columns.includes("name")) {
                throw new DuplicateNameError(name, "category");
            }
            if (columns.includes("category_id") && attempt < MAX_ID_GENERATION_ATTEMPTS) {
                continue;
            }
            throw err;
        }
    }

    // Unreachable: every loop iteration above either returns or throws. Kept so the
    // function has an explicit terminal path rather than an implicit `return undefined`
    // control-flow analysis can't rule out from the loop alone.
    throw new Error("Failed to generate a unique category id after multiple attempts.");
}

export async function renameCategory(db, id, name) {
    const now = new Date().toISOString();

    let result;
    try {
        result = await db
            .prepare("UPDATE categories SET name = ?, updated_at = ? WHERE category_id = ?")
            .bind(name, now, id)
            .run();
    } catch (err) {
        if (uniqueConstraintColumns(err, "categories").includes("name")) {
            throw new DuplicateNameError(name, "category");
        }
        throw err;
    }

    if (result.meta.changes === 0) {
        return null;
    }

    return db
        .prepare(`SELECT ${SELECT_COLUMNS} FROM categories WHERE category_id = ?`)
        .bind(id)
        .first();
}

export async function deleteCategory(db, id) {
    const result = await db
        .prepare("DELETE FROM categories WHERE category_id = ?")
        .bind(id)
        .run();
    return result.meta.changes > 0;
}
