import { describe, expect, it } from "vitest";
import { setSpanAttributes } from "./span-attributes.js";

describe("setSpanAttributes", () => {
    it("sets only the defined attributes on the span", () => {
        const calls = [];
        const getSpan = () => ({ setAttributes: (attrs) => calls.push(attrs) });

        setSpanAttributes({ "user.id": "u1", "session.id": undefined }, getSpan);

        expect(calls).toEqual([{ "user.id": "u1" }]);
    });

    it("is a no-op when there is no active span", () => {
        expect(() => setSpanAttributes({ "user.id": "u1" }, () => undefined)).not.toThrow();
    });

    it("is a no-op with the default getter when the runtime has no tracing API", () => {
        expect(() => setSpanAttributes({ "user.id": "u1" })).not.toThrow();
    });

    it("swallows an error thrown by the span", () => {
        const getSpan = () => ({
            setAttributes() {
                throw new Error("telemetry failure");
            },
        });

        expect(() => setSpanAttributes({ "user.id": "u1" }, getSpan)).not.toThrow();
    });
});
