Delivery is two releases (see the spec's Decisions). Groups 1-5 are stage one: the switch and
the code removals, against the existing database. Groups 6-7 are stage two (a cutover to a
new database) and start only after group 5 passes. Run every Worker task test-first (red, green, refactor).

## 1. Switch AdminApi to cloudfare-identity

- [x] 1.1 Write failing vitest cases in `identity.test.js` for the request: `X-Identity-Application: Trainfree.Admin`, `X-Identity-Credential` equal to `ADMIN_INTERNAL_KEY`, `X-Identity-Token` equal to the Access-supplied token (not a rewritten `CF_Authorization` cookie), and the URL path `/v1/identity`; confirm they fail for the right reason
- [x] 1.2 Write failing vitest cases for each response: `200` success shape `{ email, userId, role }`, `401` and `403` relayed, and `404`, `503`, an unexpected status and a thrown binding call all mapped to `503`; confirm red
- [x] 1.3 Rewrite `callIdentityApi` in `identity.js` as a cloudfare-identity call (rename to match, drop the `X-Trainfree-*` headers and `Cookie` forwarding, drop the `IdentityApi` wording in comments) and verify 1.1 and 1.2 pass with the existing `isValidIdentity` and `sessionId` checks untouched
- [ ] 1.4 Confirm which header carries the Access token on a request reaching `AdminApi` in production (expected `Cf-Access-Jwt-Assertion`) by logging its presence only, never its value, on a deployed preview or a temporary trace attribute; record the result in the PR, and fall back to the header name verified there
- [x] 1.5 Replace the local-dev lookup in `resolveLocalDevIdentity` with a fixed synthetic identity (no lookup in the Worker; a local-only seed of the `users` row stays in test setup and `scripts/seed-local-dev-user.sql` while the foreign key remains); verify a vitest case with `LOCAL_DEV_BYPASS` set and no identity rows passes and that a case with it unset and no binding still returns `503`
- [x] 1.6 Update the remaining specs-adjacent tests (`index.test.js`, `exercise-images.test.js`, `test/apply-migrations.js`, `vitest.config.js`) so none reads `logins`/`users`/`roles` beyond the stage-one foreign-key seed in `test/apply-migrations.js`, or stubs `IDENTITY` with the old contract; verify `npm test` in `src/Trainfree.AdminApi` is green

## 2. Bind the service and secret

- [x] 2.1 In `wrangler.jsonc` and `wrangler.deploy.jsonc`, point the `IDENTITY` service binding at the `cloudfare-identity` Worker and remove the comments describing multi-worker local dev against `trainfree-identity-api`; verify `npx wrangler deploy --dry-run --config wrangler.deploy.jsonc` accepts both files
- [x] 2.2 Confirm `ADMIN_INTERNAL_KEY` holds the same value in `trainfree-admin` and in `cloudfare-identity` without printing it (`wrangler secret list` shows the name on both); update `.dev.vars.example` to describe the new header and credential
- [x] 2.3 Verify `trainfree-admin`'s deployed Access application and `cloudfare-identity`'s registered audience for `Trainfree.Admin` match, by checking the registered application record

## 3. Retire the internal implementation

- [x] 3.1 Remove the `deploy-identity-api` job from `.github/workflows/deploy.yaml`, fix the remaining job's `needs`, and verify the file passes `actionlint` or a dry run of the workflow
- [ ] 3.2 Remove `Trainfree.IdentityApi` from the `worker-tests` matrix and comments in `.github/workflows/ci.yaml`; verify CI's required-check names still match the branch protection rule
- [x] 3.3 Remove the `IdentityApi` entry and its opt-in `Trainfree:IdentityApi` setting from `src/Trainfree.AppHost/Program.cs`; verify `dotnet build src/Trainfree.AppHost` succeeds
- [x] 3.4 Delete `src/Trainfree.IdentityApi` (including its provisioning script and smoke harness) and verify `git grep -i "IdentityApi" -- src .github` finds nothing outside historical migrations and comments that say it was retired
- [x] 3.5 Update `CLAUDE.md` (third-Worker paragraph, port 9998 mention, AppHost paragraph, Access-application paragraph), `README.md`, `.github/copilot-instructions.md` and `.pr_agent.toml`; verify none describes `IdentityApi` as current
- [x] 3.6 Update `docs/identity/*`, `docs/cloudflare-infrastructure.md`, `docs/trainfree-roadmap.md` and `docs/identity/rollout-runbook.md` to describe cloudfare-identity and the two-stage rollout; verify with `git grep -i "IdentityApi" -- docs ':!docs/pr-reviews'`
- [x] 3.7 Edit the `Purpose` text in `openspec/specs/admin-api-enforcement/spec.md` to drop the `IdentityApi` wording, and the `admin-access-gating` rationale that names `IdentityApi`; run `openspec validate --strict`

## 4. Pre-release checks

- [x] 4.1 Run the full solution and Worker suites (`dotnet test Trainfree.slnx --configuration Release`; `npm test` in `src/Trainfree.AdminApi`) and verify both are green
- [ ] 4.2 Stop for review and approval before committing; open the stage-one PR and verify CI is green

## 5. Stage-one rollout and production check

- [ ] 5.1 Merge the stage-one PR with `gh pr merge --squash`, push the next `v0.0.N` tag, and verify `deploy.yaml` succeeds and its version poll reports the tag's stamp
- [ ] 5.2 In production, sign in as the admin and verify `GET /api/me` returns `200` with the admin email and role `Administrator`, and that creating a program records `user_id` `USR-S6PHKJ`
- [ ] 5.3 Verify the no-access path in production returns `403` (a login with no `Trainfree.Admin` role) and record the result
- [ ] 5.4 Record the production check results in the stage-one PR and confirm with the owner that stage two may proceed

## 6. New database and baseline schema

- [ ] 6.1 Write a failing vitest case (real D1 binding) asserting the baseline schema: every app table exists, `logins`/`users`/`roles` do not, `programs.user_id` has no foreign key, and a NULL `user_id` insert and update both abort; confirm red
- [ ] 6.2 Replace `src/Trainfree.AdminApi/migrations/0001`-`0017` with a single baseline migration generated from the current app schema (no identity tables, no `programs.user_id` foreign key, NOT NULL triggers kept), with a comment saying the history lives in git; remove the stage-one local-dev seed (`scripts/seed-local-dev-user.sql`, its `db:seed:local` npm script, and the seed in `test/apply-migrations.js`); verify 6.1 passes and the rest of the Worker suite stays green
- [ ] 6.3 Compare the baseline against the live schema (`wrangler d1 export --remote --no-data` on the original) and verify the only differences are the removed identity tables and foreign key
- [ ] 6.4 Run the one-time `wrangler d1 create` for the new database (name it per the repo convention, e.g. `trainfree_db_v2`), record its `database_id`, and verify it appears in `wrangler d1 list`

## 7. Stage two: cutover to the new database

- [ ] 7.1 Rehearse locally: seed a local copy of the original schema with programs, sessions, session phases, program exercises and exercises rows, run the export and import into a local database built from the baseline, and record whether the import needs `PRAGMA defer_foreign_keys` or a table order; verify per-table row counts match
- [ ] 7.2 Pause use of the app, export the original's app tables only (`wrangler d1 export --remote --no-schema --table ...`, excluding `logins`, `users`, `roles`), keep the file, and verify it is non-empty and includes `programs` rows
- [ ] 7.3 Apply the baseline to the new database, import the export, and verify per-table row counts equal the original's and the program owned by `USR-S6PHKJ` keeps that `user_id`
- [ ] 7.4 Repoint `database_id` in `wrangler.jsonc` and `wrangler.deploy.jsonc` (and the database name used by `deploy.yaml`'s migrate step and any scripts or docs), leaving the original database untouched; verify `wrangler deploy --dry-run` accepts both and the Worker suite is green
- [ ] 7.5 Open the stage-two PR, squash merge, push the next `v0.0.N` tag, and verify `deploy.yaml` succeeds and its version poll passes
- [ ] 7.6 Re-run the production checks from 5.2 and 5.3 against the new database, confirm an existing program and its children load, and record the results in the stage-two PR
- [ ] 7.7 If any production check fails, repoint `database_id` back to the original database and redeploy (rollback); the original has been unmodified throughout

## 8. Cleanup and close-out

- [ ] 8.1 Delete the deployed `trainfree-identity-api` Worker, its Access application and its public hostname in Cloudflare, and verify they no longer appear in the dashboard or the API
- [ ] 8.2 After a soak period the owner chooses, delete the original `trainfree_db` (`wrangler d1 delete`) only once 7.6 has passed and the export file is archived, and verify it no longer appears in `wrangler d1 list`
- [ ] 8.3 Sync the change's deltas into `openspec/specs/` (the `identity-api` capability ends up with no requirements), delete `openspec/specs/identity-api`, delete `openspec/changes/use-cloudfare-identity/`, and verify `openspec validate --strict` and `.github/scripts/require-archived-changes.sh` pass
- [ ] 8.4 Close issue #158 referencing both PRs
