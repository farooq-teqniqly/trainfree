# trainfree

Self-hosted, single-user workout tracker. Blazor WebAssembly client + Cloudflare Worker
API, backed by D1. See `CLAUDE.md` for architecture and conventions.

## Required tooling

Install all four before your first commit. The pre-commit hook and CI both run the
linters, so a missing one is a failed commit, not a degraded check.

| Tool | Version | Used for |
| --- | --- | --- |
| .NET SDK | 10.0.x (pinned in `global.json`) | Blazor client, tests, CSharpier |
| Node.js | 22.x | Worker, `vitest`, `wrangler` |
| [actionlint](https://github.com/rhysd/actionlint/releases) | 1.7.12 | GitHub Actions workflows |
| [ShellCheck](https://github.com/koalaman/shellcheck/releases) | 0.11.0 | `.github/scripts/*.sh`, `.githooks/*` |

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

`jq` is not required by the hook, but `.github/scripts/verify-deployed-version.sh` uses it,
so you need it to run that script locally.

## Git hooks

Hooks are version-controlled in `.githooks/` and activated via `core.hooksPath`. A build
target sets this automatically; if that has not run, do it by hand once per clone:

```sh
git config core.hooksPath .githooks
```

`pre-commit` formats staged C# with CSharpier and lints staged workflow and shell files.
`commit-msg` enforces Conventional Commits.

## Local development

Two servers run side by side: the Worker (D1-backed API) and the Blazor dev server.

### 1. Worker API

```sh
cd src/Trainfree.Api
npm install         # first time only
npm run db:migrate:local   # first time only, or after adding a migration
npm run dev
```

This starts `wrangler dev` on `http://127.0.0.1:9999`. The `predev` step runs
`scripts/Kill-Port.ps1` first to clear any stuck process on that port -- `wrangler dev`
has been observed to leak orphaned listeners on port 8787 across restarts on Windows,
which is why this project pins to 9999 instead (see `wrangler.jsonc`'s `dev.port`).

### 2. Blazor client

In a second terminal, from the repo root:

```sh
dotnet run --project src/Trainfree.Web/Trainfree.Web.csproj --launch-profile http
```

Serves on `http://localhost:5280`. `appsettings.Development.json` already points the
client's API calls at `http://127.0.0.1:9999/api/`; no further setup needed.

### 3. Open the app

Navigate to `http://localhost:5280/admin` for the admin UI (programs CRUD).

## Database migrations

The API is backed by Cloudflare D1 (`trainfree_db`). Schema changes are versioned as SQL
files under `src/Trainfree.Api/migrations/`, applied with `wrangler d1 migrations`. All
commands below run from `src/Trainfree.Api`.

### Apply migrations locally

```sh
npm run db:migrate:local
```

This applies any not-yet-applied migrations to the local database -- it does not touch the
remote one, and it works without Cloudflare credentials. Run it on first checkout and after
every `git pull` that adds a migration. `wrangler` tracks which files have already run, so
re-running is a no-op.

The local database is a plain SQLite file, written by Miniflare to:

```text
src/Trainfree.Api/.wrangler/state/v3/d1/miniflare-D1DatabaseObject/
```

The `.sqlite` file with the long hex name is the database itself (`metadata.sqlite` next to
it is Miniflare's own bookkeeping, not your data). Open it with any SQLite client to inspect
tables or rows directly. The whole `.wrangler` directory is generated and git-ignored --
never commit it, and deleting it is a safe reset (see
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
rm -rf .wrangler
npm run db:migrate:local
```

## Running the tests

```sh
dotnet test Trainfree.slnx -c Release
```

```sh
cd src/Trainfree.Api
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
  migrations in the repo. Run `npm run db:migrate:local` from `src/Trainfree.Api`.
