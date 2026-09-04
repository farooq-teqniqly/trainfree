# admin-worker-deployment Specification

## Purpose
TBD - created by archiving change split-admin-workout-apps. Update Purpose after archive.
## Requirements
### Requirement: Admin Worker deployment identity
The Admin Worker SHALL deploy under the Cloudflare Worker name `trainfree-admin`,
serving `Trainfree.Admin`'s static assets and its own `/api/*` routes at that Worker's
origin.

#### Scenario: Worker name after deploy
- **WHEN** the Admin Worker is deployed via `wrangler deploy --config wrangler.deploy.jsonc`
- **THEN** it is reachable at `trainfree-admin.<account-subdomain>.workers.dev` (or the
  custom hostname mapped to that Worker), not `trainfree.<account-subdomain>.workers.dev`

### Requirement: Admin Worker version endpoint
The Admin Worker SHALL expose `GET /api/version`, reporting the same
`<tag>+<short-sha>` stamp injected into both the deployed Worker (`APP_VERSION`/
`APP_COMMIT` vars) and the compiled `Trainfree.Admin` Blazor assembly
(`InformationalVersion`), per the deploy pipeline's existing dual-stamp mechanism.

#### Scenario: Deployed stamp matches compiled stamp
- **WHEN** a deploy completes and a client calls `GET /api/version` against the Admin
  Worker
- **THEN** the response reports the same `<tag>+<short-sha>` stamp compiled into the
  `Trainfree.Admin` bundle served by that deploy

### Requirement: Admin Worker D1 binding
The Admin Worker SHALL bind the same physical D1 database (`trainfree_db`, existing
`database_id`) previously bound by the pre-split `trainfree` Worker -- no new database is
created and no data migrates.

#### Scenario: Programs data unchanged after rename
- **WHEN** the Admin Worker is deployed under its new name and a client calls
  `GET /api/programs`
- **THEN** it returns the same program rows that existed under the `trainfree` Worker
  before the rename, unchanged

### Requirement: Deprecated categories table removal
The `categories` D1 table SHALL be dropped once the Worker that replaced it with
`phases` (#60) is confirmed live in production, via a new migration
(`0009_drop_categories.sql`) rather than by editing or removing migration 0008.
**Rationale**: Migration 0008 deliberately kept `categories` so the still-deploying old
Worker could keep reading it during the deploy window; `v0.3.0` (commit 9142fa7) shipped
the Phase rename and confirms that window closed, so the deferred cleanup migration 0008
promised is now safe to apply. D1 migrations are forward-only and already-applied, so the
removal must be its own new migration, not a rewrite of history.

#### Scenario: Categories table absent after migration 0009
- **WHEN** all migrations through `0009_drop_categories.sql` have applied to `trainfree_db`
- **THEN** querying `sqlite_master` for a table named `categories` returns no row

#### Scenario: Phases table unaffected by categories removal
- **WHEN** migration `0009_drop_categories.sql` applies
- **THEN** a `phases` row previously copied from `categories` by migration 0008 is
  still present and unchanged -- the `DROP TABLE` targets only `categories`

## Decisions

- **Categories table removal (drop-categories-table, issue #67):** Chose a new
  forward-only migration (0009) over deleting the `categories` table definition from
  migration 0004 or editing migration 0008, because D1 migrations that already ran in
  production cannot be retroactively edited -- `wrangler d1 migrations apply` tracks
  applied migrations by filename/checksum, so rewriting an applied file's content or
  removing it desyncs local history from the deployed database's migration ledger. A new
  migration is the only mechanism that stays consistent with what already ran in
  production. No down-migration/rollback path is provided -- D1's migration tool has no
  built-in `down` mechanism, and a manual `CREATE TABLE categories` rollback would not
  restore the rows the table held (phases already has the copied data; the original
  `categories` rows are the only complete record of the pre-copy state, once dropped).
  This would become worth revisiting if D1 ever ships native down-migrations.

