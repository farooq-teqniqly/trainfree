export const MAX_IMAGE_BYTES = 1_048_576;

const SIGNATURES = {
    "image/jpeg": [0xff, 0xd8, 0xff],
    "image/png": [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a],
};

function normalizeContentType(declaredType) {
    if (typeof declaredType !== "string") {
        return "";
    }
    return declaredType.split(";")[0].trim().toLowerCase();
}

const UNSUPPORTED_TYPE_ERROR = "Only JPG and PNG images are supported";

/**
 * Returns the normalized supported content type for a declared type, or null when
 * it is missing or not JPEG/PNG.
 */
export function supportedImageType(declaredType) {
    const contentType = normalizeContentType(declaredType);
    return Object.hasOwn(SIGNATURES, contentType) ? contentType : null;
}

/** Returns the 415 rejection for an unsupported declared type. */
export function unsupportedTypeRejection() {
    return { valid: false, status: 415, error: UNSUPPORTED_TYPE_ERROR };
}

function startsWith(bytes, signature) {
    return bytes.length >= signature.length && signature.every((value, i) => bytes[i] === value);
}

/**
 * Validates an uploaded image body against its declared content type.
 * Returns { valid: true, contentType } with the verified type, or
 * { valid: false, status, error } carrying the HTTP status to respond with.
 */
export function validateImage(body, declaredType) {
    const contentType = supportedImageType(declaredType);

    if (!contentType) {
        return unsupportedTypeRejection();
    }
    const signature = SIGNATURES[contentType];

    const bytes = body instanceof Uint8Array ? body : new Uint8Array(body);

    if (bytes.length === 0) {
        return { valid: false, status: 400, error: "Image body is empty" };
    }

    if (bytes.length > MAX_IMAGE_BYTES) {
        return {
            valid: false,
            status: 413,
            error: `Image must be at most ${MAX_IMAGE_BYTES} bytes`,
        };
    }

    if (!startsWith(bytes, signature)) {
        return {
            valid: false,
            status: 415,
            error: "Image content does not match its declared type",
        };
    }

    return { valid: true, contentType };
}
