## ADDED Requirements

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
