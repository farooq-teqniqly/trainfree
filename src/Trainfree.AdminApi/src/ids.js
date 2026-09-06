const BODY_LENGTH = 6;
const ALPHABET = "ABCDEFGHJKMNPQRSTVWXYZ23456789";
const PROGRAM_PREFIX = "PRG-";
const SESSION_PREFIX = "SNN-";
const PHASE_PREFIX = "PHS-";
const EXERCISE_PREFIX = "EXR-";

export function generateId(prefix) {
    const randomBytes = new Uint8Array(BODY_LENGTH);
    crypto.getRandomValues(randomBytes);

    let body = "";
    for (const byte of randomBytes) {
        body += ALPHABET[byte % ALPHABET.length];
    }
    return prefix + body;
}

export function isValidId(value, prefix) {
    if (typeof value !== "string") {
        return false;
    }

    if (value.length !== prefix.length + BODY_LENGTH) {
        return false;
    }

    if (!value.startsWith(prefix)) {
        return false;
    }

    const body = value.slice(prefix.length);
    for (const char of body) {
        if (!ALPHABET.includes(char)) {
            return false;
        }
    }

    return true;
}

export function generateProgramId() {
    return generateId(PROGRAM_PREFIX);
}

export function isValidProgramId(value) {
    return isValidId(value, PROGRAM_PREFIX);
}

export function generateSessionId() {
    return generateId(SESSION_PREFIX);
}

export function isValidSessionId(value) {
    return isValidId(value, SESSION_PREFIX);
}

export function generatePhaseId() {
    return generateId(PHASE_PREFIX);
}

export function isValidPhaseId(value) {
    return isValidId(value, PHASE_PREFIX);
}

export function generateExerciseId() {
    return generateId(EXERCISE_PREFIX);
}

export function isValidExerciseId(value) {
    return isValidId(value, EXERCISE_PREFIX);
}
