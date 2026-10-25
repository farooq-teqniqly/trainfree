# trainfree

Self-hosted, single-user workout tracker. Blazor WebAssembly client + Cloudflare Worker
API, backed by D1. See `CLAUDE.md` for architecture and conventions.

## Required tooling

Install the four required tools before your first commit. The pre-commit hook and CI both
run the linters, so a missing one is a failed commit, not a degraded check.

| Tool | Version | Used for | |
| --- | --- | --- | --- |
| .NET SDK | 10.0.x (pinned in `global.json`) | Blazor client, tests, CSharpier | required |
| Node.js | 22.x | Worker, `vitest`, `wrangler` | required |
| [actionlint](https://github.com/rhysd/actionlint/releases) | 1.7.12 | GitHub Actions workflows | required |
| [ShellCheck](https://github.com/koalaman/shellcheck/releases) | 0.11.0 | `.github/scripts/*.sh`, `.githooks/*` | required |
| [jq](https://github.com/jqlang/jq/releases) | 1.8.x | running `.github/scripts/verify-deployed-version.sh` locally | optional |

The Aspire AppHost (`src/Trainfree.AppHost`) needs nothing beyond the .NET SDK and Node.js
already listed -- no Docker and no extra workload. Install the Aspire CLI only if you want
`aspire run` instead of `dotnet run`. The AppHost flow is Windows-only: each Worker's
`predev` hook runs `powershell` and `scripts/Kill-Port.ps1`, which uses Windows-only APIs,
so it fails on macOS and Linux.

The two linters are single binaries with no runtime dependencies -- unpack them anywhere on
your `PATH`. actionlint only lints the `run:` blocks inside workflows when ShellCheck is
also installed, which is why neither is optional.

### Windows

Git for Windows adds `~\bin` to the Git Bash `PATH` when it exists, which is where the
hooks run -- so this is enough for `pre-commit` to find the tools. Windows itself does not
put `~\bin` on `PATH`; to call them from PowerShell too, add it once:

```powershell
[Environment]::SetEnvironmentVariable(
    'PATH', "$env:PATH;$HOME\bin", 'User')   # restart the shell afterwards
```

```powershell
mkdir -Force ~\bin
curl.exe -sSL -o ~\bin\jq.exe https://github.com/jqlang/jq/releases/latest/download/jq-windows-amd64.exe

curl.exe -sSL -o $env:TEMP\actionlint.zip https://github.com/rhysd/actionlint/releases/download/v1.7.12/actionlint_1.7.12_windows_amd64.zip
Expand-Archive $env:TEMP\actionlint.zip $env:TEMP\actionlint -Force
Copy-Item $env:TEMP\actionlint\actionlint.exe ~\bin\

curl.exe -sSL -o $env:TEMP\shellcheck.zip https://github.com/koalaman/shellcheck/releases/download/v0.11.0/shellcheck-v0.11.0.zip
Expand-Archive $env:TEMP\shellcheck.zip $env:TEMP\shellcheck -Force
Copy-Item $env:TEMP\shellcheck\shellcheck.exe ~\bin\
```

### macOS / Linux

```sh
brew install actionlint shellcheck jq          # macOS
sudo apt-get install -y shellcheck jq          # Debian/Ubuntu (actionlint: see releases)
```

### Verify

```sh
actionlint --version && shellcheck --version && jq --version
```

The install commands above include `jq` for convenience. Nothing in the hook or the build
needs it -- only `.github/scripts/verify-deployed-version.sh` does, and CI gets it from the
runner image, so skip it unless you want to run that script locally.

## Git hooks

Hooks are version-controlled in `.githooks/` and activated via `core.hooksPath`. A build
target sets this automatically; if that has not run, do it by hand once per clone:

```sh
git config core.hooksPath .githooks
```

`pre-commit` formats staged C# with CSharpier and lints staged workflow and shell files.
`commit-msg` enforces Conventional Commits. `pre-push` runs the full solution test suite
(`dotnet test Trainfree.slnx --configuration Release`) so a fix scoped to one project
can't silently break a test elsewhere; bypass with `git push --no-verify` if it misbehaves.

## Local development

Two servers run side by side to use the admin UI: the Worker (D1-backed API) and the
Blazor dev server. `AdminApi` enforces the Administrator role on every data endpoint by
calling the shared `cloudfare-identity` Worker, but `wrangler.jsonc` sets
`LOCAL_DEV_BYPASS` by default for local dev, substituting a fixed synthetic local
Administrator identity so nothing external is called.

### One command (Aspire)

From the repo root:

```sh
dotnet run --project src/Trainfree.AppHost
```

This starts the `AdminApi` Worker (`http://127.0.0.1:9999`), the Blazor client
(`http://localhost:5280`), and the Aspire dashboard (`http://localhost:15180`), which
shows each resource's console logs and health (probed via `GET /api/version`). The launch
profile opens the dashboard in your browser; if it does not, use the login URL (with its
token) printed in the console. The dashboard shows logs and health, not traces -- the
Workers are not OpenTelemetry-instrumented. A child `admin-api-installer`
resource showing "Not started" is expected: the AppHost sets
`install: false`, so dependencies come from your own `npm install`, not Aspire. The ports
are fixed because the Blazor client reads `wwwroot/appsettings.Development.json` as a
static file, so Aspire cannot hand it a different API address.

`admin` waits for `admin-api` to pass its `/api/version` health check and has no timeout. If
`admin` stays "Waiting", open `admin-api` in the dashboard and read its console output; the
usual causes are a missing `npm install`, a port already in use, or a wrangler error.

The Worker is started with its own `npm run dev`, so its `predev` step
(`scripts/Kill-Port.ps1`) still runs under the AppHost. It clears any stale listener left
on that port by an earlier run and does not touch the AppHost-managed process.

First run still needs the one-time `npm install` and `npm run db:migrate:local`
from step 1 below -- AppHost does not run them. The AppHost is
local-only, Windows-only, not part of any deploy, and not in `Trainfree.slnx` (build it by
project path, as above) so CI and `dotnet test` skip the Aspire SDK restore.

### Manual fallback

The same stack can be run without Aspire, one terminal per process, as steps 1-2 below.

### 1. Worker API

```sh
cd src/Trainfree.AdminApi
npm install         # first time only
npm run db:migrate:local   # first time only, or after adding a migration
npm run dev
```

This starts `wrangler dev` on `http://127.0.0.1:9999`. The `predev` step runs
`scripts/Kill-Port.ps1` first to clear any stuck process on that port -- `wrangler dev`
has been observed to leak orphaned listeners on port 8787 across restarts on Windows,
which is why this project pins to 9999 instead (see `wrangler.jsonc`'s `dev.port`).

`npm run dev`/`npm run db:migrate:local` both pass `--persist-to ../.wrangler-shared`, so
the dev server and the migration command read and write the same local D1 state.

#### Exercise image storage (R2)

Exercise images live in the R2 bucket `trainfree-exercise-images`, bound to `AdminApi`
as `IMAGES` in both `wrangler.jsonc` and `wrangler.deploy.jsonc`. The bucket is not
public: the Worker serves images through `GET /api/exercises/:id/image`, so they stay
behind Cloudflare Access. Locally, Miniflare provides a real R2 with no setup and no
credentials. In production the bucket is created once, never by `deploy.yaml`:

```sh
pwsh scripts/Create-ExerciseImagesBucket.ps1   # or: powershell -File scripts/Create-ExerciseImagesBucket.ps1
```

R2 must be enabled on the Cloudflare account first (dashboard, R2 Object Storage).

### 2. Blazor client

In a second terminal, from the repo root:

```sh
dotnet run --project src/Trainfree.Admin/Trainfree.Admin.csproj --launch-profile http
```

Serves on `http://localhost:5280`. `appsettings.Development.json` already points the
client's API calls at `http://127.0.0.1:9999/api/`; no further setup needed.

### Identity service (cloudfare-identity)

Identity is resolved by the shared `cloudfare-identity` Worker, reached from `AdminApi`
over the `IDENTITY` service binding. It is not run locally: everyday dev uses
`LOCAL_DEV_BYPASS`. To exercise the real (non-bypass) path, unset `LOCAL_DEV_BYPASS` in
`wrangler.jsonc` and set `ADMIN_INTERNAL_KEY` in `AdminApi`'s `.dev.vars`
(`cp .dev.vars.example .dev.vars`); the call fails closed with `503` if it is unset.

### Cloudflare Access application (production, one-time)

`Trainfree.Admin`'s Worker is gated by a Cloudflare Access application, which
`deploy.yaml`'s post-deploy `GET /api/version` poll authenticates against. It was created
once via the Cloudflare API (self-hosted app, reusing the account's two existing reusable
Access policies rather than duplicating them):

- `trainfree-ci` (non-identity, `any_valid_service_token`) -- lets CI's service token
  through, via the `CF_ACCESS_CLIENT_ID`/`CF_ACCESS_CLIENT_SECRET` repo secrets.
- `trainfree - Production` (allow, owner's email) -- browser access for manual checks.

This is a one-time setup step, not part of the deploy pipeline (same category as
`wrangler d1 create`/`r2 bucket create` -- see `CLAUDE.md`). If it ever needs recreating,
use the Cloudflare API/dashboard, reusing those same two reusable policies by ID rather
than creating new ones.

#### ADMIN_INTERNAL_KEY (production, one-time)

`AdminApi` enforces the Administrator role on every data endpoint by calling
`cloudfare-identity` over its `IDENTITY` service binding, sending this secret as
`X-Identity-Credential` alongside `X-Identity-Application: Trainfree.Admin`. The same
value must be held by `cloudfare-identity`. This is a manual, one-time step -- like the
Access application above, it is not automated in `deploy.yaml`. Cloudflare secrets are
never readable back after they're set, so keep the value wherever you keep other
production secrets:

```sh
openssl rand -base64 32
wrangler secret put ADMIN_INTERNAL_KEY  # from src/Trainfree.AdminApi
```

Per `CLAUDE.md`'s "Prod API URL is never configured, per app" rule, this is a secret (not
a `vars` entry), so it never appears in `wrangler.jsonc`/`wrangler.deploy.jsonc`. If
`AdminApi`'s copy is missing, `GET /api/me` fails closed with a `503` without calling
`cloudfare-identity`; if the two sides' values differ, `cloudfare-identity` rejects the
call and `AdminApi` normalizes that to the same `503` -- either way the Blazor client's
`AccessGate` renders "We couldn't confirm your access," a symptom of this secret, not of
Cloudflare Access itself. `wrangler secret list` only reports whether the secret is
present, not its value, so it cannot confirm the two sides match; if both report it
present and the `503` persists, re-run `wrangler secret put ADMIN_INTERNAL_KEY` with the
same saved value on both Workers.

## Database migrations

The API is backed by Cloudflare D1 (`trainfree_db_v2`). Schema changes are versioned as SQL
files under `src/Trainfree.AdminApi/migrations/`, applied with `wrangler d1 migrations`. All
commands below run from `src/Trainfree.AdminApi`.

### Apply migrations locally

```sh
npm run db:migrate:local
```

This applies any not-yet-applied migrations to the local database -- it does not touch the
remote one, and it works without Cloudflare credentials. Run it on first checkout and after
every `git pull` that adds a migration. `wrangler` tracks which files have already run, so
re-running is a no-op.

The local database is a plain SQLite file, written by Miniflare to the shared
`--persist-to` directory `AdminApi`'s local dev uses (see
"Local development" step 1 above):

```text
src/.wrangler-shared/v3/d1/miniflare-D1DatabaseObject/
```

The `.sqlite` file with the long hex name is the database itself (`metadata.sqlite` next to
it is Miniflare's own bookkeeping, not your data). Open it with any SQLite client to inspect
tables or rows directly. The whole `.wrangler-shared` directory is generated and
git-ignored -- never commit it, and deleting it is a safe reset (see
[Reset the local database](#reset-the-local-database)).

Note that the local database is separate from the one the tests use: `vitest` applies the
same migrations to a throwaway Miniflare D1 instance on every run (see
`test/apply-migrations.js`), so `npm test` needs no migration step.

### Add a migration

```sh
npx wrangler d1 migrations create trainfree_db_v2 <short_description>
```

This creates the next numbered file (e.g. `0003_<short_description>.sql`); write the SQL
into it, then apply it locally with `npm run db:migrate:local`. Commit the migration in
the same PR as the code that depends on it.

### Apply migrations remotely

You normally do not run this by hand. `deploy.yaml` runs
`wrangler d1 migrations apply trainfree_db_v2 --remote` on every `v*.*.*` tag, before the
Worker deploy. The tag must point at a commit on `main` -- the workflow verifies this and
fails the deploy otherwise, because a single fixed Worker name means a deploy from a
feature branch would overwrite production. The manual equivalent, which needs Cloudflare
credentials, is `npm run db:migrate:remote`.

### Reset the local database

```sh
rm -rf src/.wrangler-shared
cd src/Trainfree.AdminApi
npm run db:migrate:local
```

This works unchanged with the Aspire AppHost; stop the AppHost first so no Worker holds
the shared D1 files open while they are deleted.

## OpenSpec changes

Spec-driven changes live under `openspec/`. `openspec/config.yaml`'s default schema is
the project-local `trainfree-lean` schema (issue #68), which drops `proposal.md`/
`design.md` in favor of a leaner `specs/` + `tasks.md` pair -- see
`openspec/schemas/trainfree-lean/schema.yaml`. `openspec new change <name>` uses this
default; pass `--schema spec-driven` for the stock schema instead.

### Create a new change on `trainfree-lean`

```sh
openspec new change <change-name> --schema trainfree-lean --description "<short description>"
```

This scaffolds `openspec/changes/<change-name>/` with no `proposal.md`/`design.md`. Then:

1. `openspec instructions specs --change <change-name> --json` to get the current
   `specs` artifact instruction, and write `specs/<capability-path>/spec.md` -- one
   requirement per behavior change, each with a `**Rationale**` line, plus (in the
   primary spec file, once per change) a `## Decisions` section and a
   `## Requirement coverage` table naming the change's anchor (a GitHub issue,
   `docs/trainfree-roadmap.md` slice, or `intent.md`) and mapping every anchor
   requirement to what covers it.
2. `openspec instructions tasks --change <change-name> --json`, then write `tasks.md`.
3. `openspec validate <change-name> --strict` to confirm both artifacts are complete.
4. Implement, TDD as usual.
5. Run `/opsx:gate <change-name>` before opening the PR -- it re-checks the coverage
   table against the anchor, then runs a fresh-context diff review.
6. Sync the delta into `openspec/specs/` (`openspec-sync-specs` skill, or `/opsx:sync
   <change-name>`), then delete `openspec/changes/<change-name>/` -- this schema
   replaces `openspec archive` with sync-and-delete; git history is the archive.

## Running the tests

```sh
dotnet test Trainfree.slnx -c Release
```

```sh
cd src/Trainfree.AdminApi
npm test
```

The .NET suite (xUnit + bUnit) and the Worker suite (vitest against a real
Miniflare/D1 binding, no mocking) are independent -- run both before opening a PR.

Some behavior -- e.g. `Trainfree.Admin`'s access gating against a real `AdminApi` and
`cloudfare-identity` -- is verified manually against locally running Workers rather than
(only) by the automated suites above.

### Troubleshooting

- **Requests hang / never complete**: a port conflict, not your code. Run
  `netstat -ano | findstr 9999` (Windows) to check for stray listeners, or just run
  `.\scripts\Kill-Port.ps1 -Port 9999` and restart `npm run dev`.
- **Local D1 data got messy**: wipe and reapply migrations -- see
  [Reset the local database](#reset-the-local-database).
- **API returns "no such table" or "no such column"**: your local database is behind the
  migrations in the repo. Run `npm run db:migrate:local` from `src/Trainfree.AdminApi`.
