// deploy.yaml stamps APP_VERSION (the git tag) and APP_COMMIT (short SHA) in with
// `wrangler deploy --var`, so the value reported here is always the build that is
// actually live. The Blazor client carries the same stamp compiled into its assembly
// and compares the two on startup; a mismatch means the browser is running a stale
// bundle. That failure was silent before (see issue #18).
//
// "local" is what a `wrangler dev` or test run reports, where no deploy vars exist.
export function versionStamp(env) {
    return {
        version: env.APP_VERSION ?? "local",
        commit: env.APP_COMMIT ?? "local",
    };
}
