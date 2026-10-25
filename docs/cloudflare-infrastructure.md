# Cloudflare infrastructure

Reference for the account-level Cloudflare setup this repo depends on: what exists, how
it was created, and how the pieces connect. This is a snapshot of *state*, not a
step-by-step procedure -- see `README.md` for local dev setup and
`.github/workflows/deploy.yaml` for what CI actually runs on each deploy. Project-specific
architecture rules (why one Worker per app, why no shared assets Worker, etc.) live in
`CLAUDE.md`'s "Project-specific rules" and are not repeated here.

## Workers

One Worker of this repo exists today (`trainfree-admin`); a second (`trainfree-workout`) is
planned. Identity is resolved by `cloudfare-identity`, a stand-alone Worker shared by every
app and not part of this repo. See `CLAUDE.md` for the full rationale ("Two apps, one
Worker each").

| Worker | Config | Public? | Purpose |
| --- | --- | --- | --- |
| `trainfree-admin` | `src/Trainfree.AdminApi/wrangler.jsonc` (dev) / `wrangler.deploy.jsonc` (deploy, adds `assets`) | Yes, gated by Access | Serves `Trainfree.Admin`'s Blazor static output plus `/api/*` |
| `cloudfare-identity` | outside this repo | Not used by Trainfree over its public hostname | Verifies Cloudflare Access tokens and resolves `{ email, userId, role }` for a named application, answering `GET /v1/identity` over a service binding |
| `trainfree-workout` / `Trainfree.WorkoutApi` | not yet built (roadmap slice 8+) | -- | Will mirror `trainfree-admin`'s shape for the workout runner |

`trainfree-admin` calls `cloudfare-identity` via a `services` binding
(`IDENTITY` -> `cloudfare-identity`), never over a public hostname, sending
`X-Identity-Application: Trainfree.Admin`, `X-Identity-Credential` (the
`ADMIN_INTERNAL_KEY` secret) and the user's Access token in `X-Identity-Token`.

The former in-repo `trainfree-identity-api` Worker was retired by the
`use-cloudfare-identity` change. Its deployed Worker, Access application and public
hostname are deleted manually at the end of that change's rollout.

## D1

- One physical database, `trainfree_db` (`database_id:
  f99013f2-e1c9-48d7-9f63-cfb3c853f421`), bound by `trainfree-admin` under the binding
  name `DB`; the future `trainfree-workout` Worker will bind it too once built. Not a
  secret -- committed in the Worker's `wrangler.jsonc`. The `use-cloudfare-identity`
  change's stage two cuts over to a new database without the identity tables.
- Created once with `wrangler d1 create trainfree_db`, a one-time setup step outside the
  deploy pipeline (same category as R2 bucket creation, once that's needed -- see
  `CLAUDE.md`).
- `AdminApi`'s migrations (`src/Trainfree.AdminApi/migrations/`) own every table. The
  `logins`/`users`/`roles` tables remain until stage two's baseline migration drops them;
  nothing reads them any more.
- `deploy.yaml`'s `deploy` job runs `wrangler d1 migrations apply trainfree_db --remote`
  before the Worker deploys.
- Local dev: `wrangler dev` passes `--persist-to ../.wrangler-shared` -- see README.md's
  "Worker API" section.

## R2

**Not yet provisioned.** Planned for exercise images (see `CLAUDE.md` and the roadmap's
Exercises slice): a bucket created once via `wrangler r2 bucket create`, same
one-time-setup category as D1 above, with the URL stored on the `Exercise` record in D1
rather than the image itself. No `r2_buckets` binding exists in any Worker's
`wrangler.jsonc` yet -- add this section's detail once that slice lands.

## Cloudflare Access

One self-hosted Access application exists today for this repo, provisioned manually via the
Cloudflare API/dashboard -- **not** represented as code in this repo (see `CLAUDE.md`).

| Access application | Gates | Reusable policies attached |
| --- | --- | --- |
| `trainfree-admin` | `trainfree-admin`'s public hostname (the Blazor admin UI) | `trainfree-ci` (service token), `trainfree - Production` (owner's email) |

The application uses the account's **two reusable policies** rather than its own copy:

- **`trainfree-ci`** -- `any_valid_service_token`. Lets CI's service token through for
  the post-deploy version check via `CF_ACCESS_CLIENT_ID`/`CF_ACCESS_CLIENT_SECRET`.
- **`trainfree - Production`** -- allow, owner's email. Browser access for manual checks.

If the application ever needs recreating, use the Cloudflare API/dashboard and reuse
these two reusable policies by ID -- do not create new per-app policies. Optionally set
an `APP_BASE_URL` repo variable to pin the version-poll target
instead of relying on the deploy step's own reported URL.

### Token audience

`cloudfare-identity` verifies the Access token and checks that the calling application's
audience (`Trainfree.Admin`'s registered Access application) is in the token's `aud`.
`AdminApi` does no verification itself. The registered audience must match
`trainfree-admin`'s deployed Access application; when `WorkoutApi` exists it registers
its own application and audience the same way.

## Secrets

| Secret | Where set | Used by |
| --- | --- | --- |
| `CLOUDFLARE_API_TOKEN` / `CLOUDFLARE_ACCOUNT_ID` | GitHub Actions repo secrets | `wrangler deploy`/`d1 migrations apply` in `deploy.yaml` |
| `CF_ACCESS_CLIENT_ID` / `CF_ACCESS_CLIENT_SECRET` | GitHub Actions repo secrets | CI's post-deploy `GET /api/version` poll against `trainfree-admin` (the `trainfree-ci` policy) |
| `ADMIN_INTERNAL_KEY` | `wrangler secret put` in `trainfree-admin`, one-time, manual | Sent as `X-Identity-Credential` on `AdminApi` -> `cloudfare-identity` calls; `cloudfare-identity` must hold the **same** value (see README.md's "ADMIN_INTERNAL_KEY" section) |
| `WORKOUT_INTERNAL_KEY` | not yet set | Will authenticate `WorkoutApi` -> `cloudfare-identity` calls once `WorkoutApi` exists |

The first two rows are **GitHub Actions repo secrets**, consumed by `deploy.yaml` itself
(the CI runner's credentials, never seen by the Workers). The last two are **Wrangler
secrets** set directly on the Worker (`wrangler secret put`) and read by the Worker's
own code at runtime -- neither appears in `wrangler.jsonc`/`wrangler.deploy.jsonc` as a
`vars` entry, per `CLAUDE.md`'s "Prod API URL is never configured, per app" rule.

## Observability

`trainfree-admin` enables Workers Logs
(`observability.logs`, with `invocation_logs`) and Traces
(`observability.traces`, `head_sampling_rate: 1`, `persist: true`) in its
`wrangler.jsonc`/`wrangler.deploy.jsonc`. No external log sink -- inspect via the
Cloudflare dashboard's Workers Observability tab.

### Filtering traces by user and session

Once a request's identity resolves, `trainfree-admin` sets these attributes on its root span:

- `user.id` -- the internal `userId` (never the email).
- `user.role` -- `Administrator` or `User`.
- `session.id` -- a one-way, 32-character hex value returned by `cloudfare-identity` in its
  identity response when available; it is validated and dropped if malformed.

In the Workers Observability query builder, add a filter on `user.id` to see one user's
traces, or on `session.id` to list one login's traces. Group by
either attribute to compare activity per user or per login.

`session.id` is login-level, not tab-level: an Access token lasts about 24 hours and
every tab under one login shares the same value, so narrow by time range to isolate a
single visit. A new login (even by the same user) produces a new `session.id`.

The attributes are absent, not empty, when there is nothing honest to attach: requests
that fail before identity resolves (`401`, `403`, `404`, `503`) carry no `user.*` or
`session.id`; a response without a valid `sessionId` yields no `session.id`; and there is no
`session.id` under `LOCAL_DEV_BYPASS` (no JWT). With no active span (for example under
`wrangler dev`), nothing is set and responses are unaffected. `session.id` is for
observability only and is never used for authorization.

## Deploy stamping

`deploy.yaml`'s `deploy` job (`trainfree-admin`) stamps the same value twice: once into
the Blazor assembly (`-p:InformationalVersion`) and once into the Worker
(`--var APP_VERSION/APP_COMMIT`), so the client can compare its compiled-in stamp against
the Worker's `GET /api/version`. The post-deploy step polls the live `GET /api/version` and fails if it doesn't match
what was just deployed. The stamp value is `${GITHUB_REF_NAME}+<short-sha>` -- on a tag
push this is `<tag>+<short-sha>`, but a manual `workflow_dispatch` run against `main`
stamps `main+<short-sha>` instead. See `CLAUDE.md`'s "Every deploy is stamped twice" rule
for the full mechanism, including why `Trainfree.Versioning` needs its own SHA-suffix
stripping.

## One-time setup steps (not part of the deploy pipeline)

These are run once, by hand, and are never re-run by CI:

1. `wrangler d1 create trainfree_db`
2. Create the Access application above via the Cloudflare API/dashboard, reusing the two
   reusable policies
3. `wrangler secret put ADMIN_INTERNAL_KEY` on `trainfree-admin`, with the same value
   `cloudfare-identity` holds
4. Provision at least one `Administrator` identity in `cloudfare-identity` (see
   `docs/identity/rollout-runbook.md` for the `use-cloudfare-identity` rollout)

Not yet needed: `wrangler r2 bucket create <name>` for exercise images -- add it here
once that slice lands (see the R2 section above).

## See also

- `README.md` -- local dev setup, per-app dev servers, ports
- `CLAUDE.md` -- architecture rules this infra implements
- `docs/identity/rollout-runbook.md` -- the two-stage rollout to `cloudfare-identity`
