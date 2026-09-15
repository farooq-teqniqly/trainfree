## 1. Worker scaffolding

- [x] 1.1 Create `src/Trainfree.IdentityApi/` as a sibling Worker folder (vanilla
      JavaScript, outside the .NET solution), with `package.json`, `wrangler.jsonc`,
      and a `vitest.config.js` wired to `@cloudflare/vitest-pool-workers`, mirroring
      `Trainfree.AdminApi`'s existing layout. Verify: `npm test` runs (0 tests) with
      no config errors.
- [x] 1.2 Bind the existing `trainfree_db` D1 database (same `database_id` as
      `AdminApi`'s binding) in `IdentityApi`'s `wrangler.jsonc`. Verify: `wrangler dev`
      starts locally with the D1 binding present in its startup log.
- [x] 1.3 Add a dedicated local dev port (distinct from `AdminApi`'s 9999) to
      `IdentityApi`'s `wrangler.jsonc` `dev.port` and a matching `predev`
      `Kill-Port.ps1` script, per the repo's port-9999 convention. Verify: `npm run dev`
      starts without a port conflict when `AdminApi`'s dev server is also running.

## 2. Shared provider_name constant

- [x] 2.1 Write a failing vitest test asserting a shared module exports
      `PROVIDER_NAME_CLOUDFLARE_ACCESS === "cloudflare-access"`, then add the module
      (e.g. `src/shared/providers.js`) to pass it. Verify: test goes red then green.

## 3. D1 schema migration (owned by AdminApi)

- [x] 3.1 Add a new migration file under `src/Trainfree.AdminApi/migrations/` creating
      `roles` (surrogate `id INTEGER` plus a `role_id TEXT UNIQUE` Crockford Base32
      business id, prefix `ROL-`, seeded with `Administrator` and `User` rows),
      `logins` (`UNIQUE (provider_name, provider_id)`), and `users` (surrogate
      `id INTEGER` plus a `user_id TEXT UNIQUE` business id, prefix `USR-`,
      `login_id INTEGER NOT NULL UNIQUE REFERENCES logins(id)`,
      `role_id TEXT REFERENCES roles(role_id)`) -- matching the business-id-plus-
      surrogate-id pattern already used by `programs`/`sessions`/`phases`/etc.
      Verify: `wrangler d1 migrations apply` (local) succeeds and `sqlite_master` shows
      all three tables plus two seeded `roles` rows.
- [x] 3.2 Add a second migration adding the nullable `programs.user_id TEXT
      REFERENCES users(user_id)` column, no default. Verify: existing `programs` rows
      have `user_id = NULL` after migration; inserting a `programs` row with an
      unknown `user_id` fails the FK check locally.

## 4. JWT verification and JWKS caching seam

- [x] 4.1 Write failing vitest tests for a JWKS-fetcher seam (injectable
      dependency/module) covering: JWKS served from `caches.default` when a fresh
      cached entry exists, and a fetch to Cloudflare's certs endpoint made only when
      the cache is stale/absent, honoring the response's `Cache-Control`/`max-age`
      by using a fake fetcher and locally generated JWKS, no real network calls.
      Implement the seam to pass. Verify: tests green.
- [x] 4.2 Write failing vitest tests for JWT verification: valid signature/issuer/
      audience/expiry accepted; wrong issuer rejected; expired token rejected; a
      multi-element `aud` array containing the expected audience accepted; an `aud`
      array not containing the expected audience rejected. Use test-signed JWTs
      against the fake JWKS from 4.1. Implement verification to pass. Verify: tests
      green, including the multi-element-`aud` case called out in the spec.
- [x] 4.3 Extract identity extraction behind a single provider interface/module with
      Cloudflare Access as its only implementation; no call site parses Access claims
      directly. Verify: a grep for JWT-claim field names (`email`, `aud`, `iss`)
      outside the provider module returns no hits.

## 5. Role lookup

- [x] 5.1 Write a failing vitest test seeding two `logins` rows with the same
      `provider_id` under two different `provider_name` values, asserting the role
      lookup for the Cloudflare Access identity returns the role tied to the
      `"cloudflare-access"` row specifically (using the shared constant from 2.1).
      Implement the D1 query (matching `provider_name` AND `provider_id`) to pass.
      Verify: collision test green.
- [x] 5.2 Write a failing vitest test confirming no role caching: change a seeded
      user's `role_id` in D1 between two lookups and assert the second lookup
      reflects the new role. Verify: test green with a fresh D1 query per call, no
      memoization.

## 6. /internal/identity endpoint

- [x] 6.1 Implement `GET /internal/identity` requiring `CF_Authorization`,
      `X-Trainfree-Caller`, and `X-Trainfree-Internal-Key`, with per-caller secrets
      (`ADMIN_INTERNAL_KEY`, `WORKOUT_INTERNAL_KEY` placeholder) configured as
      Wrangler secrets. Verify: `wrangler secret list` (local) shows both keys after
      `wrangler secret put`.
- [x] 6.2 Write failing vitest tests for the internal-key check ordering: missing key
      -> `404` with `{ "error": string }` JSON body, without evaluating the JWT
      (assert via a spy/seam that JWT verification was not invoked); wrong key for
      the named caller -> `404`; `admin`'s key presented for caller `workout` -> `404`.
      Implement the check-before-JWT ordering to pass. Verify: all three tests green.
- [x] 6.3 Write failing vitest tests for the full success/failure response matrix:
      valid key + valid JWT + provisioned identity -> `200` with `email`, `userId`,
      `role`; valid key + unauthenticatable JWT (missing/malformed/expired/wrong-
      issuer/wrong-audience) -> `401`; valid key + authenticated JWT with no matching
      `logins`/`users` pair -> `403`; missing/malformed `X-Trainfree-Caller` -> `401`.
      Implement to pass. Verify: all cases green, every non-200 body is
      `application/json` `{ "error": string }`.
- [x] 6.4 Write failing vitest tests for infrastructure failure handling: JWKS fetch
      throws with no cached copy -> `503`; D1 role-lookup query throws/times out ->
      `503`; assert no unhandled exception escapes as an uncaught 5xx (wrap the
      handler in a try/catch and assert on a forced-throw fake). Implement to pass.
      Verify: tests green.

## 7. Provisioning script

- [x] 7.1 Write a failing vitest test for the provisioning script's idempotency
      check: given an existing `logins` + `users` pair for a `(provider_name,
      provider_id)`, running the script again makes no D1 writes (assert via a spy on
      the D1 batch/exec call). Implement the check to pass.
- [x] 7.2 Write a failing vitest test asserting the script inserts the `logins` and
      `users` rows in a single D1 transaction/batch when no identity exists yet
      (assert both inserts are part of one `db.batch()` call, not two separate
      `db.prepare().run()` calls). Implement to pass. Verify: tests green.
- [x] 7.3 Wire the script to accept email, `provider_name` (defaulting to the shared
      `"cloudflare-access"` constant from 2.1), and role name as CLI args, looking up
      `role_id` by name (never creating a role). Verify: running the script against a
      local D1 instance with an unknown role name fails loudly rather than silently
      creating one.

## 8. CI/deploy wiring

- [x] 8.1 Restructure `deploy.yaml` to add a second deploy/verify sequence for
      `IdentityApi` (no publish step) alongside the existing `Trainfree.Admin`/
      `AdminApi` sequence, each with its own `APP_BASE_URL`-equivalent variable and
      Cloudflare Access service-token secrets. Verify: a dry run / workflow syntax
      check (`gh workflow view` or `actionlint`) shows no errors.
- [x] 8.2 Stamp `IdentityApi` with `APP_VERSION`/`APP_COMMIT` on deploy and poll its
      `GET /api/version` in CI, failing the job if the reported stamp doesn't match.
      Verify: a tagged deploy to a test/staging tag shows the version-poll step
      passing in the Actions run log.

## 9. Documentation and manual setup

- [x] 9.1 Update README.md's "Local development" section to add a third subsection
      for running `IdentityApi` locally (`cd src/Trainfree.IdentityApi`, `npm install`,
      `npm run dev` on its own dev port from task 1.3), noting it has no migration step
      of its own (it reads tables `AdminApi`'s migration owns) and, since no caller is
      wired up yet in this slice, how to exercise `/internal/identity` directly for
      manual testing (e.g. `curl` with the required headers). Verify: a developer
      following the updated steps from a clean checkout can reach `/internal/identity`
      locally and get a `404`/`401` response without consulting anything beyond the
      README.
- [x] 9.2 Amend `CLAUDE.md`'s "Two apps, one Worker each" rule to document
      `IdentityApi` as the explicit third, service-binding-only Worker exception (the
      current draft already describes this; confirm it matches what actually ships
      after tasks 1-8, adjusting wording if the implementation diverged). Verify: a
      reviewer can trace every claim in that `CLAUDE.md` paragraph to a merged file in
      this change.
- [ ] 9.3 Manually create `IdentityApi`'s own Cloudflare Access application in the
      Zero Trust dashboard, gating its public hostname, distinct from
      `Trainfree.Admin`'s. Verify: an unwhitelisted email is challenged by Access when
      browsing to `IdentityApi`'s hostname directly.
- [ ] 9.4 Document the slice 1/2 rollout runbook (deploy slice 1, run the
      provisioning script, confirm `/internal/identity` resolves via the dedicated
      smoke-harness Wrangler config with `LOCAL_DEV_BYPASS` unset, only then deploy
      slice 2) as a repo doc or PR description checklist. Verify: the doc names the
      exact smoke-harness config file path and the header/key values the check sends.

## 10. End-to-end verification

- [ ] 10.1 Run the full `IdentityApi` vitest suite and confirm every scenario listed
      in `specs/identity-api/spec.md` has a corresponding passing test. Verify:
      `npm test` green, with a manual cross-check against the spec's scenario list.
- [ ] 10.2 Deploy `IdentityApi` to a real Cloudflare environment with no caller wired
      up yet, run the provisioning script against it once, and confirm
      `/internal/identity` resolves that identity to `200`/`Administrator` via the
      smoke-harness config from 9.4 -- the exact check task 6 in the rollout order
      requires before slice 2 can be deployed. Verify: the smoke-harness script's
      output shows the real `email`/`userId` the provisioning script created, not a
      synthetic value.
