## Context

The fix is a one-line markup change plus an extension of an existing bUnit test,
following a pattern (`aria-expanded="@(cond ? "false" : "true")"`) already shipped in
`Programs.razor` and already proven by an existing test
(`Chevron_ClickOnExpandedProgram_SetsAriaExpandedFalse`). No new pattern, dependency,
or architectural decision is introduced.

## Goals / Non-Goals

**Goals:**
- Make `MainLayout.razor`'s nav toggler render `aria-expanded="true"` /
  `aria-expanded="false"` as literal strings.
- Prove it with an assertion on the attribute value, not just presence/class state.

**Non-Goals:**
- No change to `NavMenu`'s `Collapsed` parameter or the sidebar's CSS-driven
  show/hide behavior.
- No broader accessibility audit of `MainLayout.razor` or other components beyond
  this one attribute.

## Decisions

- **Reuse the existing ternary-stringification pattern** (`Programs.razor`) rather
  than introducing a helper or extension method. The pattern is a single ternary
  expression; a wrapper would be an abstraction for one call site with no other
  current user.
- **Extend `Render_ClickingTheNavToggler_ExpandsTheCollapsedSidebar` in place**
  rather than adding a new `Fact` (e.g. mirroring
  `Chevron_ClickOnExpandedProgram_SetsAriaExpandedFalse`'s name). The existing test
  already renders the layout, finds the toggler, and clicks it -- the only gap is
  which attributes it asserts before/after. Adding a second `Fact` doing the same
  click sequence to check a different attribute would be a near-duplicate test
  under the project's Theory-over-near-duplicate-Facts convention, and here even a
  `[Theory]` doesn't fit better since the two assertions (class list, aria-expanded)
  are checked at both the before and after points already covered by one flow.

## Risks / Trade-offs

- [Minimal -- single-line markup change matching a shipped pattern] → No
  significant risk; the existing test suite plus the extended assertion cover
  regression.
