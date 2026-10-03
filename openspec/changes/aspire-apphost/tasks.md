## 1. AppHost project

- [x] 1.1 Add `src/Trainfree.AppHost` (Aspire AppHost) with pinned package versions in `Directory.Packages.props`, registered in `Trainfree.slnx`
- [x] 1.2 Declare `admin-api` (port 9999), optional `identity-api` (port 9998, only when `Trainfree:IdentityApi=true`), and `admin` (port 5280, waits for `admin-api`); health probe `GET /api/version`
- [x] 1.3 Verify no `deploy.yaml`, `wrangler*.jsonc`, or Cloudflare Access change in the diff

## 2. CI and docs

- [x] 2.1 Append `**/Trainfree.AppHost/**` to `SONAR_EXCLUSIONS` in `.github/workflows/ci.yaml`; `.github/scripts/verify-path-allowlists-sync.sh` passes
- [x] 2.2 README: "One command (Aspire)" section first under Local development, existing steps relabeled "Manual fallback", Required tooling note, Reset section note
- [x] 2.3 CLAUDE.md project rule: AppHost is local-only, fixed ports (Blazor reads `appsettings.Development.json` statically), IdentityApi opt-in

## 3. Verify

- [x] 3.1 `dotnet build Trainfree.slnx -c Release` and `dotnet test Trainfree.slnx -c Release` pass; `dotnet csharpier check .` clean
- [x] 3.2 `dotnet run --project src/Trainfree.AppHost`: dashboard shows `admin-api` and `admin` healthy with logs streaming; admin UI loads at http://localhost:5280 and lists programs (after migrate + provision)
- [x] 3.3 With `-- --Trainfree:IdentityApi=true`: `identity-api` healthy on 9998 and shares D1 state with `admin-api`
- [x] 3.4 A `.dev.vars` value (e.g. `ADMIN_INTERNAL_KEY`) is picked up by wrangler under AppHost
- [x] 3.5 After stopping AppHost, `Get-NetTCPConnection -State Listen -LocalPort 9999,9998,5280` returns nothing; restart works
- [x] 3.6 Manual three-terminal flow from README still works
- [x] 3.7 `openspec validate aspire-apphost --strict` passes; run `/opsx:gate aspire-apphost` before the PR
