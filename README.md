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
Blazor dev server. `AdminApi` enforces the Administrator role on every data endpoint,
but `wrangler.jsonc` sets `LOCAL_DEV_BYPASS` by default for local dev, substituting a
synthetic local Administrator identity instead of calling `IdentityApi` -- see step 2
below for the one-time seed row this needs. A third server, `IdentityApi`, only needs
to run if you're testing the real (non-bypass) `/internal/identity` path directly.

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

`npm run dev`/`npm run db:migrate:local` both pass `--persist-to ../.wrangler-shared`,
a directory shared with `IdentityApi`'s own local dev (see step 3) -- each Worker's
`wrangler dev` otherwise creates its own separate `.wrangler/state` SQLite file even
though both bind the same `database_id`, so without a shared `--persist-to`,
`IdentityApi` would never see the `logins`/`users`/`roles` tables this migration
creates.

### 2. Seed the local-dev identity (one-time)

`identity.js`'s `LOCAL_DEV_BYPASS` branch looks up a real `users` row for
`local-dev@trainfree.local` rather than fabricating a `userId` -- without it, every
`AdminApi` data endpoint fails locally with a clear error instead of a silent bad
`userId`. Seed it once, against the same shared local D1 step 1 just migrated:

```sh
cd src/Trainfree.IdentityApi
npm install         # first time only
npm run provision -- --email local-dev@trainfree.local --role Administrator
```

This uses `IdentityApi`'s own provisioning script (see step 4 below), not `wrangler
dev` -- no server needs to be running for this command. It's idempotent: re-running it
after the first time reports "already provisioned" and makes no changes, and
[Reset the local database](#reset-the-local-database) requires re-running it.

**Never append `--remote` to this exact command.** `provision-identity.js` also accepts
`--remote` to target the deployed database (see step 4), but `local-dev@trainfree.local`
is a synthetic identity meant only for the local D1 instance -- provisioning it remotely
creates an unnecessary privileged Administrator row in production with no real Cloudflare
Access identity behind it.

### 3. Blazor client

In a second terminal, from the repo root:

```sh
dotnet run --project src/Trainfree.Admin/Trainfree.Admin.csproj --launch-profile http
```

Serves on `http://localhost:5280`. `appsettings.Development.json` already points the
client's API calls at `http://127.0.0.1:9999/api/`; no further setup needed.

### 4. IdentityApi (optional)

Not needed for everyday local dev of the admin UI -- step 2's seed row is enough for
`LOCAL_DEV_BYPASS` to resolve a real identity without `IdentityApi` running at all. Run
this only to exercise the real (non-bypass) `/internal/identity` service-binding path,
e.g. to test `identity.js` with `LOCAL_DEV_BYPASS` unset in `wrangler.jsonc`.

`IdentityApi` has no migration step of its own -- it reads the `logins`/`users`/`roles`
tables that `AdminApi`'s migration (`npm run db:migrate:local` above) owns, so run that
first if you haven't. Its own `npm run dev` also passes `--persist-to
../.wrangler-shared` (the same directory step 1 migrated), which is what actually makes
those tables visible here -- without it, `IdentityApi` would read its own separate,
empty local D1 state and every provisioning/lookup would fail with missing tables. In a
third terminal:

```sh
cd src/Trainfree.IdentityApi
npm install         # first time only
cp .dev.vars.example .dev.vars   # first time only; fill in ADMIN_INTERNAL_KEY locally
npm run dev
```

This starts `wrangler dev` on `http://127.0.0.1:9998` (`AdminApi`'s dev server can stay
running on 9999 at the same time). Exercise `/internal/identity` directly with `curl` to
confirm it's up:

```sh
curl -i http://127.0.0.1:9998/internal/identity
# -> 401, missing X-Trainfree-Caller

curl -i http://127.0.0.1:9998/internal/identity -H "X-Trainfree-Caller: admin"
# -> 404, missing/wrong X-Trainfree-Internal-Key (checked before the JWT)
```

Getting a real `200` additionally requires a valid Cloudflare Access JWT and a
provisioned identity (a run of `npm run provision` -- see
`src/Trainfree.IdentityApi/scripts/provision-identity.js`) -- the two responses above are
enough to confirm the Worker itself is running correctly.

### Cloudflare Access application (production, one-time)

`IdentityApi`'s own public hostname (`trainfree-identity-api.<workers.dev subdomain>`,
distinct from `Trainfree.Admin`'s) is gated by its own Cloudflare Access application --
this is what `deploy.yaml`'s post-deploy `GET /api/version` poll authenticates against.
It was created once via the Cloudflare API (self-hosted app, reusing the account's two
existing reusable Access policies rather than duplicating them):

- `trainfree-ci` (non-identity, `any_valid_service_token`) -- lets CI's service token
  through. It's the *same* reusable policy already attached to `trainfree-admin`, so
  `IDENTITY_API_CF_ACCESS_CLIENT_ID`/`IDENTITY_API_CF_ACCESS_CLIENT_SECRET` (the repo
  secrets `deploy.yaml`'s `deploy-identity-api` job reads) should hold the same service
  token credentials already used for `CF_ACCESS_CLIENT_ID`/`CF_ACCESS_CLIENT_SECRET`, not
  a newly minted token.
- `trainfree - Production` (allow, owner's email) -- browser access for manual checks.

This is a one-time setup step, not part of the deploy pipeline (same category as
`wrangler d1 create`/`r2 bucket create` -- see `CLAUDE.md`). If it ever needs recreating,
use the Cloudflare API/dashboard, reusing those same two reusable policies by ID rather
than creating new ones; also set an `IDENTITY_API_BASE_URL` repo variable if you want to
pin the poll's URL instead of relying on the deploy step's own output.

#### ADMIN_INTERNAL_KEY (production, one-time)

`AdminApi` enforces the Administrator role on every data endpoint by calling
`IdentityApi` over its `IDENTITY` service binding, authenticating that call with the
same internal-key secret `IdentityApi` already checks (see
`Trainfree.IdentityApi/src/identity/config.js`'s `CALLER_CONFIG`). This is a
manual, one-time step -- like the Access application above, it is not automated in
`deploy.yaml`:

```sh
wrangler secret put ADMIN_INTERNAL_KEY  # from src/Trainfree.AdminApi
wrangler secret put ADMIN_INTERNAL_KEY  # from src/Trainfree.IdentityApi
```

Both commands must be given the **same** value -- `IdentityApi` compares the value
`AdminApi` presents against its own copy. Per `CLAUDE.md`'s "Prod API URL is never
configured, per app" rule, this is a secret (not a `vars` entry), so it never appears in
either Worker's `wrangler.jsonc`/`wrangler.deploy.jsonc`.

### 5. Open the app

Navigate to `http://localhost:5280/admin` for the admin UI (programs CRUD).

## Database migrations

The API is backed by Cloudflare D1 (`trainfree_db`). Schema changes are versioned as SQL
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
`--persist-to` directory both `AdminApi` and `IdentityApi` point their local dev at (see
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
npx wrangler d1 migrations create trainfree_db <short_description>
```

This creates the next numbered file (e.g. `0003_<short_description>.sql`); write the SQL
into it, then apply it locally with `npm run db:migrate:local`. Commit the migration in
the same PR as the code that depends on it.

### Apply migrations remotely

You normally do not run this by hand. `deploy.yaml` runs
`wrangler d1 migrations apply trainfree_db --remote` on every `v*.*.*` tag, before the
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

This also wipes the local-dev identity seed row (see "Local development" step 2) --
re-run it, or every `AdminApi` data endpoint fails locally until you do:

```sh
cd src/Trainfree.IdentityApi
npm run provision -- --email local-dev@trainfree.local --role Administrator
```

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

### Troubleshooting

- **Requests hang / never complete**: a port conflict, not your code. Run
  `netstat -ano | findstr 9999` (Windows) to check for stray listeners, or just run
  `.\scripts\Kill-Port.ps1 -Port 9999` and restart `npm run dev`.
- **Local D1 data got messy**: wipe and reapply migrations -- see
  [Reset the local database](#reset-the-local-database).
- **API returns "no such table" or "no such column"**: your local database is behind the
  migrations in the repo. Run `npm run db:migrate:local` from `src/Trainfree.AdminApi`.
