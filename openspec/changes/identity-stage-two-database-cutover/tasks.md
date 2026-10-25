## 1. New database and baseline schema

- [ ] 1.1 Write a failing vitest case (real D1 binding) asserting the baseline schema: every app table exists, `logins`/`users`/`roles` do not, `programs.user_id` has no foreign key, and a NULL `user_id` insert and update both abort; confirm red
- [ ] 1.2 Replace `src/Trainfree.AdminApi/migrations/0001`-`0017` with a single baseline migration generated from the current app schema (no identity tables, no `programs.user_id` foreign key, NOT NULL triggers kept), with a comment saying the history lives in git; remove the stage-one local-dev seed (`scripts/seed-local-dev-user.sql`, its `db:seed:local` npm script, the seed in `test/apply-migrations.js`, and the `migrations.test.js` assertions about the identity tables and foreign key); verify 1.1 passes and `npm test` in `src/Trainfree.AdminApi` stays green
- [ ] 1.3 Compare the baseline against the live schema (`wrangler d1 export --remote --no-data` on the original) and verify the only differences are the removed identity tables and foreign key
- [ ] 1.4 Run the one-time `wrangler d1 create` for the new database (name it per the repo convention, e.g. `trainfree_db_v2`), record its `database_id`, and verify it appears in `wrangler d1 list`

## 2. Cutover to the new database

- [ ] 2.1 Rehearse locally: seed a local copy of the original schema with programs, sessions, session phases, program exercises and exercises rows, run the export and import into a local database built from the baseline, and record whether the import needs `PRAGMA defer_foreign_keys` or a table order; verify per-table row counts match
- [ ] 2.2 Pause use of the app, export the original's app tables only (`wrangler d1 export --remote --no-schema --table ...`, excluding `logins`, `users`, `roles`), keep the file, and verify it is non-empty and includes `programs` rows
- [ ] 2.3 Apply the baseline to the new database, import the export, and verify per-table row counts equal the original's and the program owned by `USR-S6PHKJ` keeps that `user_id`
- [ ] 2.4 Repoint `database_id` in `wrangler.jsonc` and `wrangler.deploy.jsonc` (and the database name used by `deploy.yaml`'s migrate step and any scripts or docs), leaving the original database untouched; verify `wrangler deploy --dry-run` accepts both and the Worker suite is green
- [ ] 2.5 Stop for review and approval before committing; open the stage-two PR and verify CI is green
- [ ] 2.6 Squash merge with `gh pr merge --squash`, push the next version tag, and verify `deploy.yaml` succeeds and its version poll passes
- [ ] 2.7 In production, sign in as the admin and verify `GET /api/me` returns `200` with role `Administrator`, creating a program records `user_id` `USR-S6PHKJ`, and an existing program and its children load; record the results in the stage-two PR
- [ ] 2.8 Verify the no-access path in production returns `403` (a login with no `Trainfree.Admin` role); this was not checked in stage one; record the result in the stage-two PR
- [ ] 2.9 If any production check fails, repoint `database_id` back to the original database and redeploy (rollback); the original has been unmodified throughout

## 3. Cleanup and close-out

- [ ] 3.1 Delete the deployed `trainfree-identity-api` Worker, its Access application and its public hostname in Cloudflare, and verify they no longer appear in the dashboard or the API
- [ ] 3.2 After a soak period the owner chooses, delete the original `trainfree_db` (`wrangler d1 delete`) only once 2.7 and 2.8 have passed and the export file is archived, and verify it no longer appears in `wrangler d1 list`
- [ ] 3.3 Delete `openspec/changes/identity-stage-two-database-cutover/` in the stage-two PR (no deltas to sync) and verify `.github/scripts/require-archived-changes.sh` passes
- [ ] 3.4 Close issue #158 referencing PR #159 and the stage-two PR
