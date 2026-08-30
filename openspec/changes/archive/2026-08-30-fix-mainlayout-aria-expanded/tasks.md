## 1. Test (red)

- [x] 1.1 Extend `Render_ClickingTheNavToggler_ExpandsTheCollapsedSidebar` in
      `tests/Trainfree.Admin.Tests/Layout/MainLayoutTests.cs` to also assert the
      toggler's `aria-expanded` attribute value: `"false"` before the click, `"true"`
      after -- mirroring `Chevron_ClickOnExpandedProgram_SetsAriaExpandedFalse` in
      `ProgramsPageTests.cs`. Do not add a new `Fact`.
- [x] 1.2 Run the test and confirm it fails for the right reason (the current
      `bool`-bound `aria-expanded` renders as an HTML boolean toggle, not the
      literal string).

## 2. Fix (green)

- [x] 2.1 In `src/Trainfree.Admin/Layout/MainLayout.razor`, change
      `aria-expanded="@(!_navCollapsed)"` to
      `aria-expanded="@(_navCollapsed ? "false" : "true")"`.
- [x] 2.2 Run the extended test and confirm it passes.
- [x] 2.3 Run the full `Trainfree.Admin.Tests` suite to confirm no regressions.
