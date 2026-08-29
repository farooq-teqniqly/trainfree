const PREFIX = "PRG-";
const BODY_LENGTH = 6;
const ALPHABET = "ABCDEFGHJKMNPQRSTVWXYZ23456789";

export function generateProgramId() {
    const randomBytes = new Uint8Array(BODY_LENGTH);
    crypto.getRandomValues(randomBytes);

    let body = "";
    for (const byte of randomBytes) {
        body += ALPHABET[byte % ALPHABET.length];
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
