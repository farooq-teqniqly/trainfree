export class DuplicateNameError extends Error {
    constructor(name) {
        super(`A program named "${name}" already exists.`);
        this.name = "DuplicateNameError";
    }
}

export function isUniqueConstraintError(err) {
    return err instanceof Error && /UNIQUE constraint failed/i.test(err.message);
}
