// deploy.yaml stamps APP_VERSION (the git tag) and APP_COMMIT (short SHA) in with
// `wrangler deploy --var`, so the value reported here is always the build that is
// actually live. IdentityApi has no client of its own to compare stamps against --
// this endpoint exists solely so deploy.yaml's CI step can confirm the deployed
// Worker matches the build it just deployed (see specs/identity-api/spec.md's
// "IdentityApi is stamped and polled in CI with no client banner" requirement).
//
// "local" is what a `wrangler dev` or test run reports, where no deploy vars exist.
export function versionStamp(env) {
    return {
        version: env.APP_VERSION ?? "local",
        commit: env.APP_COMMIT ?? "local",
    };
}
