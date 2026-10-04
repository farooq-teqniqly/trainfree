import { describe, expect, it, vi } from "vitest";
import { setSpanAttributes } from "./span-attributes.js";

describe("setSpanAttributes", () => {
    it("sets the defined attributes on the active span", () => {
        const span = { setAttributes: vi.fn() };

        setSpanAttributes({ "user.id": "u1", "session.id": undefined }, () => span);

        expect(span.setAttributes).toHaveBeenCalledWith({ "user.id": "u1" });
    });

    it("does not throw when there is no active span", () => {
        expect(() => setSpanAttributes({ "user.id": "u1" }, () => undefined)).not.toThrow();
    });

    it("does not throw with the default getter when the runtime has no tracing API", () => {
        expect(() => setSpanAttributes({ "user.id": "u1" })).not.toThrow();
    });

    it("swallows an error thrown by the span", () => {
        const span = {
            setAttributes: () => {
                throw new Error("boom");
            },
        };

        expect(() => setSpanAttributes({ "user.id": "u1" }, () => span)).not.toThrow();
    });
});
