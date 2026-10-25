## 1. New database and baseline schema

- [x] 1.1 Write a failing vitest case (real D1 binding) asserting the baseline schema: every app table exists, `logins`/`users`/`roles` do not, `programs.user_id` has no foreign key, and a NULL `user_id` insert and update both abort; confirm red
- [x] 1.2 Replace `src/Trainfree.AdminApi/migrations/0001`-`0017` with a single baseline migration generated from the current app schema (no identity tables, no `programs.user_id` foreign key, NOT NULL triggers kept), with a comment saying the history lives in git; remove the stage-one local-dev seed (`scripts/seed-local-dev-user.sql`, its `db:seed:local` npm script, the seed in `test/apply-migrations.js`, and the `migrations.test.js` assertions about the identity tables and foreign key); verify 1.1 passes and `npm test` in `src/Trainfree.AdminApi` stays green
- [x] 1.3 Compare the baseline against the live schema (`wrangler d1 export --remote --no-data` on the original) and verify the only differences are the removed identity tables and foreign key
- [x] 1.4 Run the one-time `wrangler d1 create` for the new database (name it per the repo convention, e.g. `trainfree_db_v2`), record its `database_id`, and verify it appears in `wrangler d1 list` (created: `trainfree_db_v2`, `database_id` `3b4d4abf-993f-43a3-ab09-f5eb1c4353fa`)

## 2. Cutover to the new database

- [x] 2.1 Rehearse locally: seed a local copy of the original schema with programs, sessions, session phases, program exercises and exercises rows, run the export and import into a local database built from the baseline, and record whether the import needs `PRAGMA defer_foreign_keys` or a table order; verify per-table row counts match
  - Rehearsal result (local only, old schema 0001-0017 seeded with 3 programs incl. one owned by `USR-S6PHKJ`, 3 sessions, 2 phases, 3 exercises, 3 session phases, 3 program exercises; baseline applied to an empty local db): the import succeeded and per-table row counts matched (3/3/2/3/3/3), the owner kept its `user_id`, `PRAGMA foreign_key_check` was clean.
  - `wrangler d1 export --no-schema` emits `PRAGMA defer_foreign_keys=TRUE;` as its first line itself, so no extra PRAGMA is needed. Table order is not needed either when that line is present (a children-first file imported fine with it) but is required without it (children-first without the PRAGMA failed with a foreign key error); the export lists parents first anyway.
  - A program row with a NULL `user_id` is rejected by the baseline trigger, so the import aborts if the original has any ownerless program: check `SELECT count(*) FROM programs WHERE user_id IS NULL` on the original before 2.2.
  - Real sequence: `npx wrangler d1 export trainfree_db --remote --no-schema --table programs --table sessions --table phases --table exercises --table session_phases --table program_exercises --output <file>.sql` (2.2), then `npx wrangler d1 migrations apply trainfree_db_v2 --remote` and `npx wrangler d1 execute trainfree_db_v2 --remote --file <file>.sql` (2.3), run from `src/Trainfree.AdminApi`. Note `d1 export` has no `--persist-to`, so rehearsals use wrangler's default local state.
- [x] 2.2 Pause use of the app, export the original's app tables only (`wrangler d1 export --remote --no-schema --table ...`, excluding `logins`, `users`, `roles`), keep the file, and verify it is non-empty and includes `programs` rows
- [x] 2.3 Apply the baseline to the new database, import the export, and verify per-table row counts equal the original's and the program owned by `USR-S6PHKJ` keeps that `user_id`
- [x] 2.4 Repoint `database_id` in `wrangler.jsonc` and `wrangler.deploy.jsonc` (and the database name used by `deploy.yaml`'s migrate step and any scripts or docs), leaving the original database untouched; verify `wrangler deploy --dry-run` accepts both and the Worker suite is green
- [ ] 2.5 Stop for review and approval before committing; open the stage-two PR and verify CI is green
- [ ] 2.6 Squash merge with `gh pr merge --squash`, push the next version tag, and verify `deploy.yaml` succeeds and its version poll passes
- [ ] 2.7 In production, sign in as the admin and verify `GET /api/me` returns `200` with role `Administrator`, creating a program records `user_id` `USR-S6PHKJ`, and an existing program and its children load; record the results in the stage-two PR
- [ ] 2.8 Verify the no-access path in production returns `403` (a login with no `Trainfree.Admin` role); this was not checked in stage one; record the result in the stage-two PR
- [ ] 2.9 If any production check fails, roll back by reverting the cutover PR with `git revert` (restoring `database_id`, the database name used by `deploy.yaml`, and migrations `0001`-`0017`) and pushing the next version tag; do not just repoint `database_id`, because the original database has the old migration names recorded as applied and the migrate step would try to run `0001_baseline.sql` against its existing tables. The original has been unmodified throughout

## 3. Cleanup and close-out

- [ ] 3.1 Delete the deployed `trainfree-identity-api` Worker, its Access application and its public hostname in Cloudflare, and verify they no longer appear in the dashboard or the API
- [ ] 3.2 After a soak period the owner chooses, delete the original `trainfree_db` (`wrangler d1 delete`) only once 2.7 and 2.8 have passed and the export file is archived, and verify it no longer appears in `wrangler d1 list`
- [ ] 3.3 Delete `openspec/changes/identity-stage-two-database-cutover/` in the stage-two PR (no deltas to sync) and verify `.github/scripts/require-archived-changes.sh` passes
- [ ] 3.4 Close issue #158 referencing PR #159 and the stage-two PR
