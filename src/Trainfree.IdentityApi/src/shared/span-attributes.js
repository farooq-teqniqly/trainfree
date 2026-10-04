import * as workers from "cloudflare:workers";

// Namespace import plus optional chaining, not `import { tracing }`: runtimes older
// than the custom-span API (including the pinned vitest-pool-workers one) don't export
// `tracing`, and a missing named export can fail module linking.
function defaultGetSpan() {
    return workers.tracing?.getActiveSpan?.();
}

// Telemetry attribution must never affect a response or authorization, so this is a
// no-op with no active span and swallows any span failure. `getSpan` is an injectable
// seam for tests, which have no real span.
export function setSpanAttributes(attributes, getSpan = defaultGetSpan) {
    try {
        const defined = Object.fromEntries(
            Object.entries(attributes).filter(([, value]) => value !== undefined),
        );
        getSpan()?.setAttributes(defined);
    } catch (err) {
        console.warn("Failed to set span attributes", err);
    }
}
