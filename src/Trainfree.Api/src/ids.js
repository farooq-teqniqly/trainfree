const PREFIX = "PRG-";
const BODY_LENGTH = 6;
const ALPHABET = "ABCDEFGHJKMNPQRSTVWXYZ23456789";

export function generateProgramId() {
    let body = "";
    for (let i = 0; i < BODY_LENGTH; i++) {
        body += ALPHABET[Math.floor(Math.random() * ALPHABET.length)];
    }
    return PREFIX + body;
}

export function isValidProgramId(value) {
    if (typeof value !== "string") {
        return false;
    }

    if (value.length !== PREFIX.length + BODY_LENGTH) {
        return false;
    }

    if (!value.startsWith(PREFIX)) {
        return false;
    }

    const body = value.slice(PREFIX.length);
    for (const char of body) {
        if (!ALPHABET.includes(char)) {
            return false;
        }
    }

    return true;
}
