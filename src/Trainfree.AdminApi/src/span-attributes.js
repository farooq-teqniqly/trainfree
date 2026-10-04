import * as workers from "cloudflare:workers";

// Namespace import plus optional chaining, not `import { tracing }`: the pinned
// @cloudflare/vitest-pool-workers runtime predates the custom-span API and exports no
// `tracing`, so a named import would not link there.
export function getActiveSpan() {
    return workers.tracing?.getActiveSpan?.();
}

// Annotates the invocation's root span for observability only -- never for authorization.
// Undefined values are skipped, and any failure is swallowed (with a warning): telemetry
// must never alter a response.
export function setSpanAttributes(attributes, getSpan = getActiveSpan) {
    try {
        const defined = Object.fromEntries(
            Object.entries(attributes).filter(([, value]) => value !== undefined),
        );
        if (Object.keys(defined).length === 0) {
            return;
        }
        getSpan()?.setAttributes(defined);
    } catch (err) {
        console.warn("Failed to set span attributes", err);
    }
}
