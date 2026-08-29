## 1. Trainfree.Domain (new shared library)

- [ ] 1.1 Create `src/Trainfree.Domain` as a plain class library (`Microsoft.NET.Sdk`,
      no Blazor reference), registered in `Trainfree.slnx` under `/src/`.
- [ ] 1.2 Move `Ids/DomainId.cs`, `Ids/CrockfordBase32.cs`, `Ids/ProgramId.cs` from
      `src/Trainfree.Web` into `src/Trainfree.Domain` (namespace updated to match).
- [ ] 1.3 Create `tests/Trainfree.Domain.Tests`, move the corresponding existing tests
      for these types out of `tests/Trainfree.Web.Tests`, register in `Trainfree.slnx`
      under `/tests/`.
- [ ] 1.4 Add `Directory.Build.props`-covered project references so
      `src/Trainfree.Web` (soon `Trainfree.Admin`) references `Trainfree.Domain`; fix up
      `using` statements at call sites.

## 2. Trainfree.Versioning (new shared library)

- [ ] 2.1 Create `src/Trainfree.Versioning` as a Razor Class Library
      (`Microsoft.NET.Sdk.Razor`), registered in `Trainfree.slnx` under `/src/`.
- [ ] 2.2 Move `Versioning/IVersionCheck.cs`, `Versioning/VersionCheck.cs`,
      `Versioning/VersionCheck.Logging.cs`, `Versioning/VersionStamp.cs`, and
      `Layout/VersionIndicator.razor` (+ `.razor.css`) from `src/Trainfree.Web` into
      `src/Trainfree.Versioning`.
- [ ] 2.3 Create `tests/Trainfree.Versioning.Tests`, move the corresponding existing
      tests out of `tests/Trainfree.Web.Tests`, register in `Trainfree.slnx`.
- [ ] 2.4 Add project reference from `Trainfree.Web` (soon `Trainfree.Admin`) to
      `Trainfree.Versioning`; update `MainLayout.razor`'s reference to
      `VersionIndicator` and any DI registration for `IVersionCheck` in `Program.cs`.

## 3. Rename Trainfree.Web -> Trainfree.Admin

- [ ] 3.1 Rename directory `src/Trainfree.Web` -> `src/Trainfree.Admin` (git mv to
      preserve history); rename `Trainfree.Web.csproj` -> `Trainfree.Admin.csproj` and
      its root namespace/assembly name.
- [ ] 3.2 Rename `tests/Trainfree.Web.Tests` -> `tests/Trainfree.Admin.Tests`
      (git mv); rename `.csproj` and root namespace/assembly name; verify it still only
      contains tests for `Trainfree.Admin`'s own code (Admin CRUD, layout, pages) after
      tasks 1 and 2 moved out the shared-library tests.
- [ ] 3.3 Update `Trainfree.slnx` to reflect both renames.
- [ ] 3.4 Update `appsettings.Development.json`'s API base address comment/value if it
      references the old project name; verify local dev (`dotnet run` against
      `Trainfree.AdminApi`'s `wrangler dev` on port 9999) still works end to end.

## 4. Rename Trainfree.Api -> Trainfree.AdminApi

- [ ] 4.1 Rename directory `src/Trainfree.Api` -> `src/Trainfree.AdminApi` (git mv).
- [ ] 4.2 Update `wrangler.jsonc`: `name` -> `trainfree-admin`; verify `d1_databases`
      binding still points at the existing `trainfree_db` `database_id` (unchanged, no
      new database).
- [ ] 4.3 Update `wrangler.deploy.jsonc`: `name` -> `trainfree-admin`;
      `assets.directory` -> `../Trainfree.Admin/bin/Release/net10.0/publish/wwwroot`;
      verify `d1_databases` binding is unchanged.
- [ ] 4.4 Update `package.json`/npm scripts (`predev`, `Kill-Port.ps1` references) and
      any hardcoded `Trainfree.Api` paths inside the Worker's own source/tests.
- [ ] 4.5 Run the Worker's `vitest` suite (`@cloudflare/vitest-pool-workers`) against
      the renamed project to confirm nothing broke in the move.

## 5. Deploy pipeline updates

- [ ] 5.1 Update `.github/workflows/deploy.yaml`: `dotnet publish` target path
      (`src/Trainfree.Admin/Trainfree.Admin.csproj`), the stray-`_redirects` cleanup
      path, `workingDirectory` for the migrate/deploy steps
      (`src/Trainfree.AdminApi`).
- [ ] 5.2 Update `.github/scripts/verify-deployed-version.sh` if it hardcodes the
      `trainfree` Worker name or URL anywhere beyond the `APP_BASE_URL`/
      `deployment-url` inputs already parameterized.
- [ ] 5.3 Confirm the `APP_BASE_URL` repository variable still resolves correctly for
      the renamed Worker, or flag for the user to update it (see task 7).

## 6. Documentation

- [ ] 6.1 Rewrite CLAUDE.md's "Project-specific rules" section: replace the "Two
      stacks, one Worker" description with the two-Worker-per-app architecture (one
      Worker per app, each with its own D1 binding to the shared `trainfree_db`
      database, `Trainfree.Domain`/`Trainfree.Versioning` as shared libraries, no
      combined asset directory), including the not-yet-built
      `Trainfree.Workout`/`Trainfree.WorkoutApi` half so the doc matches the intended
      end state.
- [ ] 6.2 Update `docs/trainfree-roadmap.md`'s slice 2 (`split-admin-workout-apps`)
      description to match what was actually built, and mark it **Done** per the
      convention slice 1 uses.
- [ ] 6.3 Update `docs/trainfree-proposal.md`'s "Two client apps, one Worker" section
      (status line and body) to reflect the two-Worker decision instead of the
      combined-assets-directory sketch that was superseded.

## 7. Manual/operational follow-ups (outside repo code)

- [ ] 7.1 Reconfigure the Cloudflare Access application/policy (dashboard-only) to
      cover the new `trainfree-admin` hostname before relying on Access-gated access
      to the renamed Worker.
- [ ] 7.2 Confirm or update the `APP_BASE_URL` GitHub repository variable to point at
      the renamed Worker's hostname.

## 8. Verification

- [ ] 8.1 `dotnet build`/`dotnet test` the full solution locally; confirm
      `Trainfree.Admin`, `Trainfree.Domain`, `Trainfree.Versioning` and their test
      projects all build and pass.
- [ ] 8.2 Push a `v0.0.N` tag and confirm `deploy.yaml` succeeds end to end: publish,
      D1 migrations apply, deploy, and `verify-deployed-version.sh` passes against the
      live `trainfree-admin` Worker.
- [ ] 8.3 Manually smoke-test `Trainfree.Admin`'s program CRUD against the renamed,
      redeployed Worker to confirm no behavior regressed.
- [ ] 8.4 Re-run the `coverage-report` skill after the rename/extraction and compare
      against the pre-change baseline in `docs/coverage-analysis/COVERAGE_ANALYSIS_2026-08-28.md`
      (91.4% line / 82.3% branch, 56 methods). Line/branch coverage should be unchanged --
      moving files and their tests into `Trainfree.Domain`/`Trainfree.Versioning`/
      `Trainfree.Admin.Tests` should carry coverage with them, not drop it. Investigate
      any drop before merging.
