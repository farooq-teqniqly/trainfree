const MIN_NAME_LENGTH = 4;
const MAX_NAME_LENGTH = 100;

export function validateName(name) {
    if (typeof name !== "string") {
        return { valid: false, error: "name is required" };
    }

    const trimmed = name.trim();

    if (trimmed.length < MIN_NAME_LENGTH || trimmed.length > MAX_NAME_LENGTH) {
        return {
            valid: false,
            error: `name must be between ${MIN_NAME_LENGTH} and ${MAX_NAME_LENGTH} characters`,
        };
    }

    return { valid: true, name: trimmed };
}

export function validateProgramName(name) {
    return validateName(name);
}

export function validateSessionName(name) {
    return validateName(name);
}

export function validateCategoryName(name) {
    return validateName(name);
}
