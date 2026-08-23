## 1. claude-templates (separate repo, lands first)

Per the "update the template first, then re-sync" rule, none of section 2 is valid until
this section merges in `farooq-teqniqly/claude-templates`.

- [ ] 1.1 Add `CLAUDE-blazor-ui.md` with sections: Asset delivery, Icons, Navigation and
      accessibility, Testability, Editable rows, Styling. No project-specific values --
      no palette, no typeface, no version pins.
- [ ] 1.2 Add an `-IncludeBlazorUi` switch to `Initialize-Project.ps1` that copies the
      module and appends its `@CLAUDE-blazor-ui.md` import, matching how `-IncludeDdd`
      and `-IncludePm` already work. The switch implies `-IncludeDdd`, since the module
      cross-references it for typed outcomes.
- [ ] 1.3 Confirm `param()` is still the first statement in `Initialize-Project.ps1` and
      that no em dashes or smart quotes entered any string literal (Windows PowerShell 5.1
      reads UTF-8-without-BOM as Windows-1252 and corrupts them).
- [ ] 1.4 Add a `CLAUDE-blazor-ui.md` row to the README layout table, and a note in the
      module list that it pairs with the DDD module.
- [ ] 1.5 Run `Initialize-Project.ps1` against a scratch directory with `-IncludeBlazorUi`
      and confirm the module is copied and the import appears in the generated `CLAUDE.md`.

## 2. Trainfree conventions

- [ ] 2.1 Copy `CLAUDE-blazor-ui.md` from claude-templates into the repo root, byte for
      byte. It carries no `{{PROJECT_NAME}}` token, so no substitution applies.
- [ ] 2.2 Add `@CLAUDE-blazor-ui.md` to `CLAUDE.md`'s import block, after
      `@CLAUDE-domain-driven-design.md`.
- [ ] 2.3 Add two bullets to `CLAUDE.md`'s Project-specific rules carrying the values the
      shared module deliberately omits: CDN pins with SRI and no vendored `wwwroot/lib`
      copy; the Bootstrap dashboard shell (dark sticky navbar, light `bg-body-tertiary`
      sidebar), Inter as the brand typeface, and black `.btn-primary` (`#000`, `#1a1a1a`
      on hover) overridden in `app.css`.
- [ ] 2.4 Re-read `CLAUDE.md` end to end and confirm no bullet restates something the
      shared module already says -- the repo file holds values and overrides only.

## 3. Decision record

- [ ] 3.1 Add `docs/ui-decisions.md` covering, for each decision, what was chosen and why:
      CDN over vendored Bootstrap (#31), Inter (#31), the dashboard shell (#32), the empty
      `<a href="">` bug and why `NavLink` is mandatory (#32), Bootstrap Icons over inline
      SVG (#33), black primary and the mobile toggle position (#34), and the Save/Revert
      dirty-row affordance (#35). Cite the PR number for each.
- [ ] 3.2 Link `docs/ui-decisions.md` from `CLAUDE.md`'s "What this is" paragraph, next to
      the existing proposal/mockups/roadmap references.

## 4. ProgramRow refactor

Non-behavioral: the baseline exempts non-behavioral edits from writing a failing test
first, and `ProgramsPageTests` already covers the dirty/save/revert paths this touches.
No new tests -- 4.2 is the safety net.

- [ ] 4.1 Replace `ProgramRow`'s primary constructor in
      `src/Trainfree.Web/Pages/Admin/Programs.razor` with an explicit constructor using
      `_field` backing fields, so `name` no longer stays in scope where `SavedName` is
      meant. `Id` stays get-only; `Name` and `SavedName` stay settable; `IsDirty` keeps
      its `StringComparison.Ordinal` comparison.
- [ ] 4.2 Run `dotnet test` and confirm `ProgramsPageTests` passes unchanged -- an
      assertion change here would mean the refactor was not behavior-preserving.
- [ ] 4.3 Run `dotnet csharpier check .` (or the `check` subcommand as configured) and
      confirm the file is formatted; the pre-commit hook enforces this.

## 5. Spec backfill

The `Revert` affordance shipped in #35 without an OpenSpec change, so `specs/programs`
documents `Save` visibility but not `Revert`. No code changes -- this brings the spec up
to behavior that is already live and already tested.

- [ ] 5.1 Confirm the four new `Revert` scenarios in the change's spec delta match what
      `ProgramsPageTests` already asserts, and that none describes behavior the code does
      not have. If a scenario has no corresponding test, either add the test or drop the
      scenario -- do not spec unverified behavior.

## 6. Verification and follow-on

- [ ] 6.1 Confirm `CLAUDE-blazor-ui.md` is identical in both repos (`git diff --no-index`
      against the claude-templates copy, empty output).
- [ ] 6.2 Confirm no rule in the new module contradicts `CLAUDE-baseline.md` or
      `CLAUDE-domain-driven-design.md`. The primary-constructor conflict resolved in
      section 4 is the known instance; check the file for others before merge.
- [ ] 6.3 File a pr-center issue: bump Bootstrap 5.3.3 to match, add the missing `integrity`
      hash on the Bootstrap CSS and JS links in
      `src/PrCenter.Web/Components/App.razor`, and sync `CLAUDE-blazor-ui.md`. Explicitly
      out of scope here (see design.md Non-Goals) -- the issue is what keeps it from
      drifting further.
- [ ] 6.4 Run `openspec archive add-blazor-ui-conventions -y` before merge --
      `openspec-archive-check` fails any PR leaving a change under `openspec/changes/`.
