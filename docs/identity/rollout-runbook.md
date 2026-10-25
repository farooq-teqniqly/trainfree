# Rollout runbook: switch to cloudfare-identity

Procedure for the `use-cloudfare-identity` change (issue #158), which replaces the in-repo
`IdentityApi` Worker with the shared `cloudfare-identity` Worker and then moves program
data to a database without identity tables. It ships in two releases so each can be
verified, and rolled back, on its own. The change's `tasks.md` is the checklist; this
runbook records the order and the reasons. For the current state of Cloudflare
infrastructure, see `docs/cloudflare-infrastructure.md`.

The previous slice-1/2 rollout (deploying the old `IdentityApi` before `AdminApi`
enforced it) is superseded and kept only in git history.

## Preconditions

- `cloudfare-identity` already holds the existing administrator, with `userId`
  `USR-S6PHKJ` preserved, and a registered application `Trainfree.Admin` whose audience
  matches `trainfree-admin`'s Access application.
- `ADMIN_INTERNAL_KEY` has the same value in `trainfree-admin` and `cloudfare-identity`
  (`wrangler secret list` shows the name on both; values are never printed).
- The `IDENTITY` service binding in `wrangler.jsonc` and `wrangler.deploy.jsonc` points at
  `cloudfare-identity`.

## Stage one: the switch (existing database)

1. Confirm which header carries the Access token on a request reaching `AdminApi` in
   production (expected `Cf-Access-Jwt-Assertion`), logging presence only, never the
   value. Record the result in the stage-one PR.
2. Merge the stage-one PR (squash), push the next `v0.0.N` tag, and confirm `deploy.yaml`
   succeeds and its version poll reports the tag's stamp. The change also removes the
   `deploy-identity-api` job and `Trainfree.IdentityApi`.
3. In production, sign in as the administrator: `GET /api/me` returns `200` with the admin
   email and role `Administrator`, and creating a program records `user_id` `USR-S6PHKJ`.
4. Verify the no-access path: a login with no `Trainfree.Admin` role gets `403`.
5. Record the results in the stage-one PR and confirm with the owner before stage two.

If step 3 or 4 fails, redeploy the previous tag; the database is untouched in this stage.

## Stage two: cutover to a new database

1. Build the baseline schema (no identity tables, no `programs.user_id` foreign key,
   NOT NULL triggers kept) as a single migration and compare it with the live schema
   (`wrangler d1 export --remote --no-data`); the only differences
   must be the removed tables and foreign key.
2. Create the new database once with `wrangler d1 create` (for example `trainfree_db_v2`)
   and record its `database_id`. This is a one-time setup step, never part of the deploy.
3. Rehearse locally: import an export of the app tables into a database built from the
   baseline, note whether `PRAGMA defer_foreign_keys` or a table order is needed, and
   check per-table row counts.
4. Pause use of the app, export the original's app tables only (excluding `logins`,
   `users`, `roles`), keep the file, apply the baseline to the new database, import, and
   verify row counts and that the `USR-S6PHKJ` program keeps its `user_id`.
5. Repoint `database_id` and the database name used by `deploy.yaml`'s migrate step,
   leaving the original database untouched. Open the stage-two PR, merge, tag, and
   confirm the deploy and version poll.
6. Re-run the production checks from stage one and confirm an existing program and its
   children load.

Rollback: repoint `database_id` back to the original database and redeploy. The original
is unmodified throughout.

## Cleanup

- Delete the deployed `trainfree-identity-api` Worker, its Access application and its
  public hostname.
- After a soak period of the owner's choosing, and only once stage two's checks have
  passed and the export file is archived, delete the original `trainfree_db`.
- Sync the change's deltas into `openspec/specs/` and delete the change directory.
