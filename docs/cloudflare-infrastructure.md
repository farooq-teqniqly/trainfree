# Cloudflare infrastructure

Reference for the account-level Cloudflare setup this repo depends on: what exists, how
it was created, and how the pieces connect. This is a snapshot of *state*, not a
step-by-step procedure -- see `README.md` for local dev setup and
`.github/workflows/deploy.yaml` for what CI actually runs on each deploy. Project-specific
architecture rules (why three Workers, why no shared assets Worker, etc.) live in
`CLAUDE.md`'s "Project-specific rules" and are not repeated here.

## Workers

Two Workers exist today (`trainfree-admin`, `trainfree-identity-api`); a third
(`trainfree-workout`) is planned. All bind one physical D1 database. See `CLAUDE.md` for
the full rationale ("Two apps, one Worker each").

| Worker | Config | Public? | Purpose |
| --- | --- | --- | --- |
| `trainfree-admin` | `src/Trainfree.AdminApi/wrangler.jsonc` (dev) / `wrangler.deploy.jsonc` (deploy, adds `assets`) | Yes, gated by Access | Serves `Trainfree.Admin`'s Blazor static output plus `/api/*` |
| `trainfree-identity-api` | `src/Trainfree.IdentityApi/wrangler.jsonc` | Yes, gated by its own Access application, but reached only by CI's version poll -- never by a browser in normal use | Verifies Cloudflare Access JWTs, looks up D1 role, answers `GET /internal/identity` over a service binding |
| `trainfree-workout` / `Trainfree.WorkoutApi` | not yet built (roadmap slice 8+) | -- | Will mirror `trainfree-admin`'s shape for the workout runner |

`trainfree-admin` calls `trainfree-identity-api` via a `services` binding
(`IDENTITY` -> `trainfree-identity-api`), never over the public hostname -- see
`docs/identity/identity-intent-01-identityapi.md` for why the public path can't work for
this call (audience mismatch, plus the caller may not be Access-whitelisted).

## D1

- One physical database, `trainfree_db` (`database_id:
  f99013f2-e1c9-48d7-9f63-cfb3c853f421`), bound by both existing Workers
  (`trainfree-admin`, `trainfree-identity-api`) under the binding name `DB`; the future
  `trainfree-workout` Worker will bind it too once built. Not a secret -- committed in
  each Worker's `wrangler.jsonc`.
- Created once with `wrangler d1 create trainfree_db`, a one-time setup step outside the
  deploy pipeline (same category as R2 bucket creation, once that's needed -- see
  `CLAUDE.md`).
- Ownership is per-table, not per-Worker: `AdminApi`'s migrations
  (`src/Trainfree.AdminApi/migrations/`) own `programs`/`sessions`/`phases`/`exercises`/
  `session_phases`/`program_exercises` *and* `logins`/`users`/`roles` (added for
  identity). `IdentityApi` reads/writes the identity tables only and has no migration
  history of its own.
- `deploy.yaml`'s `deploy` job runs `wrangler d1 migrations apply trainfree_db --remote`
  before either Worker deploys, and `deploy-identity-api` depends on that job (`needs:
  deploy`) so identity tables exist before `trainfree-identity-api` goes live.
- Local dev: both Workers' `wrangler dev` pass `--persist-to ../.wrangler-shared` so they
  see the same local SQLite state despite each otherwise creating its own
  `.wrangler/state` -- see README.md's "Worker API" and "IdentityApi (optional)"
  sections.

## R2

**Not yet provisioned.** Planned for exercise images (see `CLAUDE.md` and the roadmap's
Exercises slice): a bucket created once via `wrangler r2 bucket create`, same
one-time-setup category as D1 above, with the URL stored on the `Exercise` record in D1
rather than the image itself. No `r2_buckets` binding exists in any Worker's
`wrangler.jsonc` yet -- add this section's detail once that slice lands.

## Cloudflare Access

Two self-hosted Access applications exist today, both provisioned manually via the
Cloudflare API/dashboard -- **not** represented as code in this repo (see `CLAUDE.md`).

| Access application | Gates | Reusable policies attached |
| --- | --- | --- |
| `trainfree-admin` | `trainfree-admin`'s public hostname (the Blazor admin UI) | `trainfree-ci` (service token), `trainfree - Production` (owner's email) |
| `trainfree-identity-api` | `trainfree-identity-api`'s public hostname, reached only by CI's `GET /api/version` poll | Same two reusable policies, reused by ID |

Both applications share the account's **two reusable policies** rather than each getting
its own copy:

- **`trainfree-ci`** -- `any_valid_service_token`. Lets CI's service token through for
  the post-deploy version check. `trainfree-admin`'s deploy uses
  `CF_ACCESS_CLIENT_ID`/`CF_ACCESS_CLIENT_SECRET`; `trainfree-identity-api`'s deploy
  reuses those same two secrets rather than minting a separate credential (see
  `deploy.yaml`'s `deploy-identity-api` job comments).
- **`trainfree - Production`** -- allow, owner's email. Browser access for manual checks.

If either application ever needs recreating, use the Cloudflare API/dashboard and reuse
these two reusable policies by ID -- do not create new per-app policies. Optionally set
an `APP_BASE_URL` / `IDENTITY_API_BASE_URL` repo variable to pin the version-poll target
instead of relying on the deploy step's own reported URL.

### Per-caller JWT audiences

`IdentityApi` verifies each caller's Access JWT against a **caller-specific** audience,
not a shared allowlist -- an Admin-side identity must not be able to reuse a JWT to gain
Workout-side access. This is configured in `src/Trainfree.IdentityApi/wrangler.jsonc`'s
`vars`:

- `ACCESS_TEAM_DOMAIN` -- the Cloudflare Access team domain, checked as the JWT's issuer.
- `ADMIN_AUDIENCE` -- `trainfree-admin`'s Access application's audience tag.
- `WORKOUT_AUDIENCE` -- placeholder (`REPLACE_WITH_...`) until `WorkoutApi` and its own
  Access application exist (roadmap slice 8+).

`deploy-identity-api`'s "Verify no placeholder Access config values remain" step fails
the deploy if `ACCESS_TEAM_DOMAIN`/`ADMIN_AUDIENCE` are still placeholders;
`WORKOUT_AUDIENCE` is deliberately excluded from that check so `IdentityApi` stays
deployable before `WorkoutApi` exists.

## Secrets

| Secret | Where set | Used by |
| --- | --- | --- |
| `CLOUDFLARE_API_TOKEN` / `CLOUDFLARE_ACCOUNT_ID` | GitHub Actions repo secrets | `wrangler deploy`/`d1 migrations apply` in `deploy.yaml` |
| `CF_ACCESS_CLIENT_ID` / `CF_ACCESS_CLIENT_SECRET` | GitHub Actions repo secrets | CI's post-deploy `GET /api/version` poll, against both `trainfree-admin` and `trainfree-identity-api` (same service token, both apps' `trainfree-ci` policy) |
| `ADMIN_INTERNAL_KEY` | `wrangler secret put` in each Worker, one-time, manual | Authenticates `AdminApi` -> `IdentityApi` service-binding calls; both Workers must be given the **same** value (see README.md's "ADMIN_INTERNAL_KEY" section) |
| `WORKOUT_INTERNAL_KEY` | not yet set | Will authenticate `WorkoutApi` -> `IdentityApi` calls once `WorkoutApi` exists |

The first two rows are **GitHub Actions repo secrets**, consumed by `deploy.yaml` itself
(the CI runner's credentials, never seen by the Workers). The last two are **Wrangler
secrets** set directly on each Worker (`wrangler secret put`) and read by the Worker's
own code at runtime -- neither appears in `wrangler.jsonc`/`wrangler.deploy.jsonc` as a
`vars` entry, per `CLAUDE.md`'s "Prod API URL is never configured, per app" rule.

## Observability

Both `trainfree-admin` and `trainfree-identity-api` enable Workers Logs
(`observability.logs`, with `invocation_logs`) and Traces
(`observability.traces`, `head_sampling_rate: 1`, `persist: true`) in their
`wrangler.jsonc`/`wrangler.deploy.jsonc`. No external log sink -- inspect via the
Cloudflare dashboard's Workers Observability tab.

## Deploy stamping

`deploy.yaml`'s `deploy` job (`trainfree-admin`) stamps the same value twice: once into
the Blazor assembly (`-p:InformationalVersion`) and once into the Worker
(`--var APP_VERSION/APP_COMMIT`), so the client can compare its compiled-in stamp against
the Worker's `GET /api/version`. `deploy-identity-api` has no Blazor client, so it only
stamps the Worker vars -- there's no assembly-side comparison for `IdentityApi`. Both
jobs' post-deploy step polls the live `GET /api/version` and fails if it doesn't match
what was just deployed. The stamp value is `${GITHUB_REF_NAME}+<short-sha>` -- on a tag
push this is `<tag>+<short-sha>`, but a manual `workflow_dispatch` run against `main`
stamps `main+<short-sha>` instead. See `CLAUDE.md`'s "Every deploy is stamped twice" rule
for the full mechanism, including why `Trainfree.Versioning` needs its own SHA-suffix
stripping.

## One-time setup steps (not part of the deploy pipeline)

These are run once, by hand, and are never re-run by CI:

1. `wrangler d1 create trainfree_db`
2. Create the two Access applications above via the Cloudflare API/dashboard, reusing
   the two reusable policies
3. `wrangler secret put ADMIN_INTERNAL_KEY` against both `trainfree-admin` and
   `trainfree-identity-api`, with the same value
4. Provision at least one `Administrator` identity in D1 (see
   `src/Trainfree.IdentityApi/scripts/provision-identity.js` and
   `docs/identity/rollout-runbook.md`, archived, for the historical first-rollout
   sequencing of this step)

Not yet needed: `wrangler r2 bucket create <name>` for exercise images -- add it here
once that slice lands (see the R2 section above).

## See also

- `README.md` -- local dev setup, per-app dev servers, ports
- `CLAUDE.md` -- architecture rules this infra implements
- `docs/identity/identity-intent-01-identityapi.md` -- the design rationale behind the
  Access/audience/service-binding scheme
- `docs/identity/rollout-runbook.md` (archived) -- the one-time slice-1/2 rollout order
