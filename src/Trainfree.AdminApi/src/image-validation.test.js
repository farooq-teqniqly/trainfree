import { describe, expect, it } from "vitest";
import { MAX_IMAGE_BYTES, validateImage } from "./image-validation.js";

const PNG_SIGNATURE = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
const JPEG_SIGNATURE = [0xff, 0xd8, 0xff];

function bytesOf(signature, length = signature.length + 8) {
    const bytes = new Uint8Array(length);
    bytes.set(signature);
    return bytes;
}

describe("validateImage", () => {
    it.each([
        ["PNG", "image/png", PNG_SIGNATURE],
        ["JPEG", "image/jpeg", JPEG_SIGNATURE],
    ])("accepts a %s body with a matching declared type", (_label, declared, signature) => {
        const result = validateImage(bytesOf(signature), declared);

        expect(result).toEqual({ valid: true, contentType: declared });
    });

    it("accepts a declared type with parameters and mixed case", () => {
        const result = validateImage(bytesOf(PNG_SIGNATURE), "Image/PNG; charset=binary");

        expect(result).toEqual({ valid: true, contentType: "image/png" });
    });

    it("accepts an ArrayBuffer body", () => {
        const result = validateImage(bytesOf(PNG_SIGNATURE).buffer, "image/png");

        expect(result).toEqual({ valid: true, contentType: "image/png" });
    });

    it.each([
        ["webp", "image/webp"],
        ["svg", "image/svg+xml"],
        ["gif", "image/gif"],
        ["inherited object key", "constructor"],
        ["inherited proto key", "__proto__"],
        ["missing", null],
        ["empty", ""],
    ])("rejects an unsupported declared type (%s) with 415", (_label, declared) => {
        const result = validateImage(bytesOf(PNG_SIGNATURE), declared);

        expect(result.valid).toBe(false);
        expect(result.status).toBe(415);
        expect(result.error).toBeTypeOf("string");
    });

    it.each([
        ["a PNG declared as JPEG", PNG_SIGNATURE, "image/jpeg"],
        ["a JPEG declared as PNG", JPEG_SIGNATURE, "image/png"],
        ["unknown bytes declared as PNG", [0x00, 0x01, 0x02, 0x03], "image/png"],
        ["unknown bytes declared as JPEG", [0x00, 0x01, 0x02, 0x03], "image/jpeg"],
        ["a truncated PNG signature", PNG_SIGNATURE.slice(0, 4), "image/png"],
    ])("rejects %s with 415", (_label, signature, declared) => {
        const result = validateImage(bytesOf(signature, signature.length), declared);

        expect(result.valid).toBe(false);
        expect(result.status).toBe(415);
        expect(result.error).toBeTypeOf("string");
    });

    it("rejects an empty body with 400", () => {
        const result = validateImage(new Uint8Array(0), "image/png");

        expect(result.valid).toBe(false);
        expect(result.status).toBe(400);
        expect(result.error).toBeTypeOf("string");
    });

    it("accepts a body of exactly 1,048,576 bytes", () => {
        const result = validateImage(bytesOf(PNG_SIGNATURE, 1_048_576), "image/png");

        expect(MAX_IMAGE_BYTES).toBe(1_048_576);
        expect(result).toEqual({ valid: true, contentType: "image/png" });
    });

    it("rejects a body of 1,048,577 bytes with 413", () => {
        const result = validateImage(bytesOf(PNG_SIGNATURE, 1_048_577), "image/png");

        expect(result.valid).toBe(false);
        expect(result.status).toBe(413);
        expect(result.error).toBeTypeOf("string");
    });
});
