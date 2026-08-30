## Why

`MainLayout.razor`'s mobile nav toggler binds `aria-expanded` to a `bool` C#
expression. Blazor renders a `bool`-typed attribute expression as an HTML boolean
toggle (present-with-empty-value or omitted) rather than stringifying it, so the
attribute never carries the literal `"true"`/`"false"` that `aria-expanded` requires
-- assistive tech can never read the toggler's actual state. GH issue #47. The
identical bug was already fixed with the same pattern in `Programs.razor`'s session
chevron (PR #46); this line predates that PR and was filed separately since it was
out of scope there.

## What Changes

- `src/Trainfree.Admin/Layout/MainLayout.razor`: change the nav toggler's
  `aria-expanded` binding from `@(!_navCollapsed)` to a ternary that stringifies
  explicitly (`@(_navCollapsed ? "false" : "true")`), matching the pattern already
  used in `Programs.razor`.
- `tests/Trainfree.Admin.Tests/Layout/MainLayoutTests.cs`: extend the existing
  `Render_ClickingTheNavToggler_ExpandsTheCollapsedSidebar` test in place to also
  assert the toggler's `aria-expanded` attribute value ("true" before the click,
  "false" after) -- no new near-duplicate test.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `admin-shell`: the mobile nav toggler's `aria-expanded` attribute must render as
  the literal string `"true"`/`"false"` reflecting the sidebar's expanded/collapsed
  state, not an HTML boolean toggle.

## Impact

- Affected code: `src/Trainfree.Admin/Layout/MainLayout.razor` (one line),
  `tests/Trainfree.Admin.Tests/Layout/MainLayoutTests.cs` (one existing test
  extended).
- No API, schema, or dependency changes. Purely a markup/accessibility correctness
  fix plus a stronger assertion on an existing test.
