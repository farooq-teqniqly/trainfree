# Coverage Analysis — Trainfree

**Date:** 2026-08-28
**Branch:** split-admin-workout-apps
**Tests:** 76 passed, 0 failed
**Methods:** 56 total, 0 exceed CRAP threshold (30)

Baseline taken before any code changes on this branch (only `openspec/changes/`
artifacts exist for `split-admin-workout-apps`). This is the number to hold after that
change's rename/extraction (`Trainfree.Web` -> `Trainfree.Admin`,
`Trainfree.Api` -> `Trainfree.AdminApi`, new `Trainfree.Domain`/`Trainfree.Versioning`
libraries) lands -- moving files and their tests into sibling projects should not change
these figures.

## Summary

| Metric | Value | Threshold | Status |
|--------|-------|-----------|--------|
| Line Coverage | 91.4% | 80% | ✅ |
| Branch Coverage | 82.3% | 70% | ✅ |
| Total Methods | 56 | — | — |
| Flagged Methods (CRAP > 30) | 0 | — | ✅ |
| Flagged Methods (CRAP > 5) | 8 | — | ⚠️ |

## Risk Hotspots (Top 15 by CRAP Score)

| Rank | Class | Method | File | Complexity | Line Cov | Branch Cov | CRAP |
|------|-------|--------|------|-----------|----------|------------|------|
| 1 | `ProgramsApiClient/<GetProgramsAsync>d__4` | MoveNext | Admin/ProgramsApiClient.cs | 4 | 0% | — | **20** |
| 2 | `Versioning.VersionStamp` | .cctor | Versioning/VersionStamp.cs | 4 | 0% | — | **20** |
| 3 | `Pages.Admin.Programs` | BuildRenderTree | Pages/Admin/Programs.razor | 14 | 100% | — | 14 |
| 4 | `ProgramsApiClient/<ReadErrorAsync>d__8` | MoveNext | Admin/ProgramsApiClient.cs | 14 | 100% | 50% | **14** |
| 5 | `Pages.Admin.Programs/<RenameAsync>d__12` | MoveNext | Pages/Admin/Programs.razor | 10 | 100% | — | 10 |
| 6 | `Program/<<Main>$>d__0` | MoveNext | Program.cs | 2 | 0% | — | **6** |
| 7 | `Versioning.VersionCheck/<CheckAsync>d__5` | MoveNext | Versioning/VersionCheck.cs | 6 | 100% | — | 6 |
| 8 | `Ids.DomainId` | IsValid | Ids/DomainId.cs | 6 | 100% | — | 6 |
| 9 | `Pages.Admin.Programs/<AddProgramAsync>d__9` | MoveNext | Pages/Admin/Programs.razor | 4 | 100% | — | 4 |
| 10 | `Pages.Admin.Programs/<DeleteAsync>d__14` | MoveNext | Pages/Admin/Programs.razor | 4 | 100% | — | 4 |
| 11 | `Layout.MainLayout` | OnParametersSet | Layout/MainLayout.razor | 4 | 100% | — | 4 |
| 12 | `ProgramsApiClient/<DeleteProgramAsync>d__7` | MoveNext | Admin/ProgramsApiClient.cs | 4 | 100% | — | 4 |
| 13 | `Versioning.VersionStamp` | FromInformationalVersion | Versioning/VersionStamp.cs | 4 | 100% | — | 4 |
| 14 | `Ids.CrockfordBase32` | IsValidBody | Ids/CrockfordBase32.cs | 4 | 100% | — | 4 |
| 15 | `ProgramsApiClient/<CreateProgramAsync>d__5` | MoveNext | Admin/ProgramsApiClient.cs | 2 | 100% | — | 2 |

## Coverage Gaps

| File | Method | Line Cov | Branch Cov | Gap |
|------|--------|:--------:|:----------:|-----|
| Program.cs | Main | 0% | 0% | 12 uncovered line(s) |
| Admin/ProgramsApiClient.cs | GetProgramsAsync | 0% | 0% | 8 uncovered line(s) |
| Versioning/VersionStamp.cs | .cctor | 0% | 0% | 6 uncovered line(s) |
| Layout/VersionIndicator.razor | Reload | 0% | 0% | 1 uncovered line(s) |
| Admin/ProgramsApiClient.cs | ReadErrorAsync | 100% | 50% | 7/14 branches |

## Recommendations

**1. ProgramsApiClient.GetProgramsAsync (CRAP 20, 0% line)**
The happy-path list call has no test exercising a successful `GET /api/programs`
response deserialization; add one asserting the returned `ProgramSummary` collection.

**2. VersionStamp .cctor (CRAP 20, 0% line)**
The static initializer that builds the compiled-in stamp is untested; add a test
asserting the default/fallback stamp shape when no `InformationalVersion` is set.

**3. ProgramsApiClient.ReadErrorAsync (CRAP 14, 100% line / 50% branch)**
One of the error-body parsing branches (likely the malformed-JSON vs. plain-text
fallback path) isn't exercised; add a test hitting the uncovered branch.

**4. Program.cs Main (CRAP 6, 0% line)**
Untested application entry point (DI/host bootstrap). Typically excluded from coverage
gates rather than tested directly -- low priority, consider a coverlet exclusion instead
of a test if it keeps showing up here.

**5. VersionIndicator.Reload (CRAP 2, 0% line)**
Single-line JS-interop button handler (`location.reload()`); low complexity, low
priority to add a test for.

## Reports

| Type | Path |
|------|------|
| Cobertura XML | TestResults/coverage-analysis/raw/7bd03be1-2a23-45af-bf01-adbd529b697a/coverage.cobertura.xml |
| HTML | Not generated (pass --html to enable) |
| Text Summary | Not generated (pass --html to enable) |
| CSV | Not generated (pass --html to enable) |
