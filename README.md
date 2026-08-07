# trainfree

Self-hosted, single-user workout tracker. Blazor WebAssembly client + Cloudflare Worker
API, backed by D1. See `CLAUDE.md` for architecture and conventions.

## Local development

Two servers run side by side: the Worker (D1-backed API) and the Blazor dev server.

### 1. Worker API

```
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

```
dotnet run --project src/Trainfree.Web/Trainfree.Web.csproj --launch-profile http
```

Serves on `http://localhost:5280`. `appsettings.Development.json` already points the
client's API calls at `http://127.0.0.1:9999/api/`; no further setup needed.

### 3. Open the app

Navigate to `http://localhost:5280/admin` for the admin UI (programs CRUD).

## Running the tests

```
dotnet test Trainfree.slnx -c Release
```

```
cd src/Trainfree.Api
npm test
```

The .NET suite (xUnit + bUnit) and the Worker suite (vitest against a real
Miniflare/D1 binding, no mocking) are independent -- run both before opening a PR.

### Troubleshooting

- **Requests hang / never complete**: a port conflict, not your code. Run
  `netstat -ano | findstr 9999` (Windows) to check for stray listeners, or just run
  `.\scripts\Kill-Port.ps1 -Port 9999` and restart `npm run dev`.
- **Local D1 data got messy**: wipe and reapply migrations --
  `rm -rf src/Trainfree.Api/.wrangler && cd src/Trainfree.Api && npm run db:migrate:local`.
