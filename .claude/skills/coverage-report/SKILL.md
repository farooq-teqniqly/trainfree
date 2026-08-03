---
name: coverage-report
description: Run coverage analysis for the solution and write a dated Markdown report to docs/coverage-analysis/. Calls Run-Coverage.ps1 (co-located in this skill folder) to collect Cobertura XML, then runs CRAP-score and gap analysis, then writes the doc. Use when asked to "run coverage", "update coverage doc", "coverage report", or "check CRAP scores".
---

Generate a fresh coverage report for the solution and write it to `docs/coverage-analysis/`.

Invoke with: `/coverage-report [--output <path>] [--crap-threshold <n>] [--top <n>]`

Defaults:
- `--output`: `docs/coverage-analysis/COVERAGE_ANALYSIS_<YYYY-MM-DD>.md` (today's date)
- `--crap-threshold`: `5`
- `--top`: `15`

---

## Execution steps

### Step 1 — Run coverage

Run `Run-Coverage.ps1` (co-located with this SKILL.md) with `-Clean` to get a fresh Cobertura XML:

```powershell
# Windows PowerShell 5.1 only accepts two Join-Path segments at a time —
# chain calls rather than passing 3+ path segments in one call.
$repoRoot = & git rev-parse --show-toplevel
$skillDir = Join-Path $repoRoot ".claude"
$skillDir = Join-Path $skillDir "skills"
$skillDir = Join-Path $skillDir "coverage-report"
& (Join-Path $skillDir "Run-Coverage.ps1") -Clean
```

Capture the line starting with `COBERTURA_PATH:` from stdout. That path is `$coberturaXml`.

If exit code > 1: stop and report the build error. If exit code == 1: proceed with a warning that some tests failed.

### Step 2 — CRAP scores

Run the bundled script from the `dotnet-test:coverage-analysis` plugin skill:

```powershell
$scripts = (Get-ChildItem -Path "$env:USERPROFILE\.claude" -Filter "Compute-CrapScores.ps1" -Recurse | Select-Object -First 1).DirectoryName

& "$scripts\Compute-CrapScores.ps1" `
    -CoberturaPath @($coberturaXml) `
    -CrapThreshold <crap-threshold> `
    -TopN <top>
```

Parse output lines:
- `OVERALL_LINE_COVERAGE:<n>` → `$lineCov`
- `OVERALL_BRANCH_COVERAGE:<n>` → `$branchCov`
- `TOTAL_METHODS:<n>` → `$totalMethods`
- `FLAGGED_METHODS:<n>` → `$flaggedMethods`
- `HOTSPOTS:<json>` → parse as JSON array → `$hotspots`

### Step 3 — Coverage gaps

```powershell
& "$scripts\Extract-MethodCoverage.ps1" `
    -CoberturaPath @($coberturaXml) `
    -CoverageThreshold 100 `
    -BranchThreshold 100 `
    -Filter below-threshold
```

Parse JSON output → `$gaps`.

### Step 4 — Write the doc

Resolve output path. If `--output` not supplied use:
`<repo-root>/docs/coverage-analysis/COVERAGE_ANALYSIS_<YYYY-MM-DD>.md`

where `<repo-root>` is `& git rev-parse --show-toplevel`.

Get today's date via `(Get-Date -Format 'yyyy-MM-dd')` and current branch via `git rev-parse --abbrev-ref HEAD`.

Write the file using this template (fill every placeholder from collected data):

```markdown
# Coverage Analysis — <solution name>

**Date:** <YYYY-MM-DD>
**Branch:** <branch>
**Tests:** <passed> passed, 0 failed
**Methods:** <totalMethods> total, <methods-exceeding-crap-30> exceed CRAP threshold (30)

## Summary

| Metric | Value | Threshold | Status |
|--------|-------|-----------|--------|
| Line Coverage | <lineCov>% | 80% | <pass/fail emoji> |
| Branch Coverage | <branchCov>% | 70% | <pass/fail emoji> |
| Total Methods | <totalMethods> | — | — |
| Flagged Methods (CRAP > 30) | <n> | — | <pass/fail emoji> |
| Flagged Methods (CRAP > <threshold>) | <flaggedMethods> | — | <warn if >0> |

## Risk Hotspots (Top <top> by CRAP Score)

<If all hotspots are 100% line covered, note: "All flagged methods fully covered — scores reflect complexity only.">

| Rank | Class | Method | File | Complexity | Line Cov | Branch Cov | CRAP |
|------|-------|--------|------|-----------|----------|------------|------|
<one row per hotspot; bold CRAP cell when line coverage < 100%>

## Coverage Gaps

<If no gaps: "No methods below 100% coverage.">
<Else:>

| File | Method | Line Cov | Branch Cov | Gap |
|------|--------|:--------:|:----------:|-----|
<one row per gap; Gap = "N uncovered line(s)" or "N/M branches">

## Recommendations

<One bullet per gap that has CRAP > 5 or uncovered lines, in priority order:>

**N. Class.Method (CRAP X, Y% line)**
<One sentence: which path is uncovered, what test to add, expected impact.>

<If no actionable gaps:>
No gaps requiring attention — all complex methods are fully covered.

## Reports

| Type | Path |
|------|------|
| Cobertura XML | <path relative to repo root> |
| HTML | Not generated (pass --html to enable) |
| Text Summary | Not generated (pass --html to enable) |
| CSV | Not generated (pass --html to enable) |
```

### Step 5 — Report to user

Print:
- Output path written
- Line and branch coverage
- Hotspot count and top CRAP score
- Top 1–2 recommendations if gaps exist
