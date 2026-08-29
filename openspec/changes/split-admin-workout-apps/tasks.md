## 1. Trainfree.Domain (new shared library)

- [x] 1.1 Create `src/Trainfree.Domain` as a plain class library (`Microsoft.NET.Sdk`,
      no Blazor reference), registered in `Trainfree.slnx` under `/src/`.
- [x] 1.2 Move `Ids/DomainId.cs`, `Ids/CrockfordBase32.cs`, `Ids/ProgramId.cs` from
      `src/Trainfree.Web` into `src/Trainfree.Domain` (namespace updated to match).
      `ProgramId` had to become `public` (was `internal`) -- it's now genuinely
      consumed across an assembly boundary by a real dependent, not just a test
      project, which is exactly CLAUDE-baseline's accessibility exception.
      `DomainId`/`CrockfordBase32` stay `internal`, used only inside `Trainfree.Domain`.
- [x] 1.3 Create `tests/Trainfree.Domain.Tests`, move the corresponding existing tests
      for these types out of `tests/Trainfree.Web.Tests`, register in `Trainfree.slnx`
      under `/tests/`.
- [x] 1.4 Add `Directory.Build.props`-covered project references so
      `src/Trainfree.Web` (soon `Trainfree.Admin`) references `Trainfree.Domain`; fix up
      `using` statements at call sites.

## 2. Trainfree.Versioning (new shared library)

- [x] 2.1 Create `src/Trainfree.Versioning` as a Razor Class Library
      (`Microsoft.NET.Sdk.Razor`), registered in `Trainfree.slnx` under `/src/`. Needed
      its own `_Imports.razor` with `@using Microsoft.AspNetCore.Components.Web` --
      without it `@onclick="Reload"` compiled as literal markup text instead of an
      event binding (caught by a SonarAnalyzer S1144 "unused method" build error,
      which was the correct signal: the button was genuinely dead).
- [x] 2.2 Move `Versioning/IVersionCheck.cs`, `Versioning/VersionCheck.cs`,
      `Versioning/VersionCheck.Logging.cs`, `Versioning/VersionStamp.cs`,
      `Versioning/VersionCheckOutcome.cs` (missing from this task's original wording --
      required by `IVersionCheck`'s return type, moved alongside the rest), and
      `Layout/VersionIndicator.razor` (+ `.razor.css`) from `src/Trainfree.Web` into
      `src/Trainfree.Versioning`. `IVersionCheck`, `VersionCheck`, `VersionStamp`, and
      `VersionCheckOutcome` (+ its 3 subtypes) had to become `public` (were `internal`)
      for the same cross-assembly reason as `ProgramId` in task 1.2.
      **Verified**: `VersionStamp.Current` reads `typeof(VersionStamp).Assembly`,
      which is now `Trainfree.Versioning.dll` rather than the app's own assembly. A
      real publish (`-p:InformationalVersion=v9.9.9+abcdef1`) confirmed MSBuild
      propagates that global property to the referenced project, and confirmed
      `VersionStamp.Current` still resolves to `Version=v9.9.9 Commit=abcdef1` end to
      end -- `Trainfree.Versioning.csproj` lacks the `IncludeSourceRevisionInInformationalVersion=false`
      override so its own attribute carries an extra `.{full-sha}` suffix, but
      `VersionStamp.FromInformationalVersion`'s existing full-SHA stripping (already
      covered by an existing test case) already absorbs it correctly. No code change
      needed.
- [x] 2.3 Create `tests/Trainfree.Versioning.Tests`, move the corresponding existing
      tests out of `tests/Trainfree.Web.Tests`, register in `Trainfree.slnx`.
- [x] 2.4 Add project reference from `Trainfree.Web` (soon `Trainfree.Admin`) to
      `Trainfree.Versioning`; update `MainLayout.razor`'s reference to
      `VersionIndicator` and any DI registration for `IVersionCheck` in `Program.cs`.

## 3. Rename Trainfree.Web -> Trainfree.Admin

- [x] 3.1 Rename directory `src/Trainfree.Web` -> `src/Trainfree.Admin` (git mv to
      preserve history); rename `Trainfree.Web.csproj` -> `Trainfree.Admin.csproj` and
      its root namespace/assembly name. `git mv` on the directory itself failed with
      "Permission denied" (a background `dotnet` build-server process held a file
      handle); a plain filesystem `mv` succeeded and `git add -A` picked up the
      renames via similarity detection instead, with the same end result. Also updated
      `wwwroot/index.html`'s `<title>`/`Trainfree.Web.styles.css` link and
      `manifest.webmanifest`'s `name`/`short_name` (still said "Trainfree.Web",
      not caught by a namespace-only rename) -- now "Trainfree Admin" /
      `Trainfree.Admin.styles.css`.
- [x] 3.2 Rename `tests/Trainfree.Web.Tests` -> `tests/Trainfree.Admin.Tests`
      (git mv); rename `.csproj` and root namespace/assembly name; verify it still only
      contains tests for `Trainfree.Admin`'s own code (Admin CRUD, layout, pages) after
      tasks 1 and 2 moved out the shared-library tests. Confirmed: 76 tests still pass
      across all four test projects (18 Domain, 22 Versioning, 36 Admin) after the
      rename -- same total as the pre-change baseline.
- [x] 3.3 Update `Trainfree.slnx` to reflect both renames.
- [x] 3.4 Checked `appsettings.Development.json` -- no project-name references, no
      change needed. Local-dev end-to-end verified after task 4: started
      `trainfree-admin`'s `wrangler dev` on port 9999, confirmed `GET /api/version` and
      `GET /api/programs` both respond correctly with the existing seeded D1 data.

## 4. Rename Trainfree.Api -> Trainfree.AdminApi

- [x] 4.1 Rename directory `src/Trainfree.Api` -> `src/Trainfree.AdminApi`. Same
      "Permission denied" issue as task 3.1's `git mv` (a stale `.git/index.lock` from
      that earlier failure, no live git process) -- plain `mv` + `git add -A` again
      picked up clean renames via similarity detection.
- [x] 4.2 Updated `wrangler.jsonc`: `name` -> `trainfree-admin`; `d1_databases`
      binding unchanged (still `trainfree_db`, same `database_id`). Also refreshed a
      trailing comment that referenced the superseded "One Worker deployment serves
      both" architecture.
- [x] 4.3 Updated `wrangler.deploy.jsonc`: `name` -> `trainfree-admin`;
      `assets.directory` -> `../Trainfree.Admin/bin/Release/net10.0/publish/wwwroot`;
      `d1_databases` binding unchanged. Same stale-architecture comment refreshed here
      too.
- [x] 4.4 Updated `package.json`'s `name` -> `trainfree-admin-api`. `predev`'s
      `../../scripts/Kill-Port.ps1` reference and `Kill-Port.ps1` itself are
      parameterized by port with no project-name hardcoding -- no change needed. No
      hardcoded `Trainfree.Api` paths found inside the Worker's own source/tests.
- [x] 4.5 Ran the Worker's `vitest` suite against the renamed project: 53/53 tests
      pass (4 test files). Some benign Windows-only `EBUSY` warnings during Miniflare's
      post-run temp-dir cleanup, unrelated to the rename.

## 5. Deploy pipeline updates

- [x] 5.1 Updated `.github/workflows/deploy.yaml`: `dotnet publish` target path
      (`src/Trainfree.Admin/Trainfree.Admin.csproj`), the stray-`_redirects` cleanup
      path, `workingDirectory` for the migrate/deploy steps (`src/Trainfree.AdminApi`),
      and two stale comments referencing the old project name/architecture.
- [x] 5.2 `verify-deployed-version.sh` has no hardcoded Worker name/URL functionally
      (fully parameterized via `APP_BASE_URL`/`deployment-url`) -- updated two
      illustrative example URLs in comments/error text from `trainfree.example.*` to
      `trainfree-admin.example.*` for accuracy.
- [x] 5.3 `APP_BASE_URL` repository variable update is a manual GitHub-side step --
      flagged in task 7.2, not automatable here.
- [x] 5.4 (found during apply, not in original plan) `.github/workflows/ci.yaml`'s
      `worker-tests` job hardcoded `src/Trainfree.Api` in three places
      (`cache-dependency-path`, two `working-directory`s) -- would have broken CI.
      Updated to `src/Trainfree.AdminApi`.
- [x] 5.5 (found during apply) `README.md`'s local-dev, migration, and test-running
      instructions hardcoded `src/Trainfree.Api` (7 occurrences) and
      `src/Trainfree.Web/Trainfree.Web.csproj` (1) -- updated all to match the renamed
      projects.
- [x] 5.6 (found during apply) `.editorconfig`'s `[src/Trainfree.Web/**.cs]` CA2007
      suppression stopped matching after the rename, which silently re-enabled the rule
      and surfaced ~12 new warnings on the next full rebuild (caught, not a false
      alarm -- confirmed the rename was the cause, not new code). Updated the glob to
      `[{src/Trainfree.Admin,src/Trainfree.Versioning}/**.cs]`, extending the same
      single-threaded-WASM rationale to `Trainfree.Versioning` since it's a Razor
      component library consumed exclusively by Blazor WASM apps. Rebuilt clean: 0
      warnings, 0 errors, all 76 tests still pass.

## 6. Documentation

- [x] 6.1 Rewrote CLAUDE.md's "Project-specific rules" section: replaced "Two stacks,
      one Worker" with "Two apps, one Worker each" describing the two-Worker-per-app
      architecture, `Trainfree.Domain`/`Trainfree.Versioning` as shared libraries, and
      the not-yet-built `Trainfree.Workout`/`Trainfree.WorkoutApi` half. Also fixed the
      "Prod API URL" bullet's paths/rationale and the "stamped twice" bullet's
      `Trainfree.Admin.csproj` reference plus a note on the cross-assembly
      `VersionStamp.Current` behavior verified in task 2.2.
- [x] 6.2 Updated `docs/trainfree-roadmap.md`'s slice 2 description to match what was
      actually built (two-Worker architecture, shared libraries, deferred
      `Trainfree.Workout`/`WorkoutApi`) and marked it **Done**.
- [x] 6.3 Updated `docs/trainfree-proposal.md`: retitled "Two client apps, one Worker"
      to "Two client apps, two Workers" and rewrote the body for the two-Worker
      decision (noting the single-Worker sketch it supersedes and why); updated the
      one-pager's stack summary line and the "Two Blazor WASM projects" Key Decision to
      match.

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
