## Context

`Trainfree.Admin` currently renders the unmodified Blazor Bootstrap dashboard template
(`MainLayout.razor`, `NavMenu.razor`) plus one hand-built page (`Pages/Admin/Programs.razor`)
that uses a plain `<table class="table w-auto">` with the working/saved-value Save/Revert
pattern from `CLAUDE-blazor-ui.md`. `docs/design/admin-mockups/*.dc.html` are static,
already-reviewed hi-fi references for the target look; they are not a clickable prototype,
and their CSS is scoped to each standalone `.dc.html` file's own `<style>` block rather than
factored for reuse, so nothing there can be copy-pasted wholesale into `app.css` -- class
names and structure translate, but the mockups' `:root` custom properties and full
8-column `.sheet` table (which includes the Category/Exercise/Type/Reps/Weight/Sets/Rest/
Side/Note columns from slice 7) need to be pared down to the 1-column Program/Session
name-only subset this slice actually has data for.

This slice was scoped in `/opsx:explore` (see conversation); the five behavioral questions
below were resolved there and are treated as settled, not open, in this design.

## Goals / Non-Goals

**Goals:**
- Match the mockups' navbar/sidebar/Home/spreadsheet chrome for the Program/Session
  subset that exists today.
- Add chevron expand/collapse state for sessions under a program.
- Rename `/admin` to `/programs` and drop the redundant `Admin` sidebar link.
- Keep `Trainfree.Versioning`'s `VersionIndicator` a single, shared component -- restyle
  it in place rather than forking a second version for the navbar pill look.

**Non-Goals:**
- No Worker route, D1 schema, or API contract changes.
- No Category/Exercise library pages or picker UI (slices 5-7) -- their sidebar links
  and Home tiles render but are inert.
- No change to the Save/Revert *interaction model* -- only its visual presentation.
  (The mockup's "changes save on blur" page copy is not implemented; that copy is
  aspirational/incorrect for this slice.)
- No offline/local-first behavior (already deferred repo-wide per `CLAUDE.md`).

## Decisions

### 1. Save/Revert interaction model is unchanged; only restyled
`Programs.razor` keeps its `ProgramRow`/`SessionRow` working-value/saved-value/`IsDirty`
shape exactly as-is. The visual change is swapping the current always-visible
`<button class="btn btn-sm ...">` markup for the mockup's `icon-btn`/`icon-btn.save`/
`icon-btn.revert`/`icon-btn.danger` treatment inside `sheet`-styled `<tr>`/`<td>`
elements. `data-testid` attributes are preserved unchanged so existing bUnit tests keep
passing without edits (this is a pure presentation swap from the test's perspective).

**Alternative considered**: adopt click-to-edit/blur-save per the mockup's page-sub
copy. Rejected -- explicitly decided against during exploration; it's a behavior change
disguised as a style change, and the existing pattern already has full test coverage
that a blur-save model would invalidate.

### 2. Expand/collapse is new per-program client state
Add `HashSet<ProgramId> _collapsedIds` to `Programs.razor`'s `@code` block (default:
empty, i.e. every program starts expanded, matching the mockup's "Workout A" example and
most closely matching today's always-expanded behavior so nothing regresses on first
load). A chevron click toggles membership; sessions render conditionally on
`!_collapsedIds.Contains(row.Id)`. No API call, no persistence across page loads --
purely client-side UI state, consistent with `CLAUDE-blazor-ui.md`'s "Editable rows"
scope (that convention doesn't cover this, so it's new, minimal state, not a reused
pattern).

**Alternative considered**: persist collapsed state (e.g. `localStorage`). Rejected as
unnecessary scope for a single-user local admin tool; can be added later if it's ever
missed.

### 3. Route rename and file relocation
`Programs.razor`'s `@page` directive changes from `/admin` to `/programs`. Since
`src/Trainfree.Admin/Pages/Admin/` was named for the now-removed "Admin" wrapper concept
(not a domain grouping), the file moves to `src/Trainfree.Admin/Pages/Programs.razor`
(namespace follows folder structure per `CLAUDE-baseline.md`'s IDE0130 rule). Update
`@using Trainfree.Admin.Admin` references in the file itself; check for any other
references to that namespace (e.g. test project) before merging.

**Alternative considered**: keep the file under `Pages/Admin/` and just change the
`@page` route string. Rejected -- leaving a domain-meaningless folder name after the
concept it named is gone creates exactly the kind of drift `CLAUDE-baseline.md`'s
folder/namespace matching rule exists to prevent.

### 4. Home page becomes a real tile-grid component
`Home.razor` is rewritten (not extended) to render the `Home.dc.html` tile grid: a
`Programs` tile with a live `NavLink` to `/programs`, and `Categories`/`Exercises` tiles
rendered in the same visual style but without a `NavLink` (plain non-interactive markup,
or a `NavLink` to a route that doesn't exist yet -- resolved in favor of **no link at
all**, so there's no dead route to 404 on; a disabled-looking tile communicates "not yet"
more honestly than a broken link). Tile counts ("4 programs" in the mockup) are **not**
implemented this slice -- no requirement or API exists yet to source that count
correctly, and a hardcoded or wrong number is worse than omitting it; the tile shows the
label and description only.

### 5. `VersionIndicator` restyle stays single-instance, in the navbar
`VersionIndicator.razor` (in `Trainfree.Versioning`, shared with the future
`Trainfree.Workout`) gets its `<span class="version-stamp">` wrapped/restyled to the
`.version-pill` look (check icon + version text) via CSS in `Trainfree.Admin`'s
`app.css` (component stays presentation-agnostic; the pill's specific navy/green colors
are an Admin-app brand choice, not necessarily what Workout will want, so styling by
class name from the consuming app's stylesheet -- not a `.razor.css` scoped file inside
`Trainfree.Versioning` -- keeps that choice non-binding on Workout). The `Home.dc.html`
mockup's second copy (bottom-of-page `.meta-row`) is dropped; the mockup file itself is
corrected to match (see tasks.md).

### 6. Icon sourcing: `bi-*` with one scoped exception
All icons other than the navbar brand mark use `bi-*` classes (already CDN-loaded per
`CLAUDE.md`). The brand mark ships as inline SVG matching the mockup's dumbbell glyph,
which is the one documented exception to `CLAUDE-blazor-ui.md`'s icon-font rule (see
tasks.md for the doc update that records this durably, not just for this slice).

### 7. Reuse Bootstrap components/utilities; the mockup CSS is a visual reference, not a source
The `.dc.html` files are static design references (per their own README), not a
component library to port wholesale. Where Bootstrap (already loaded via CDN per
`CLAUDE.md`) already has a construct that produces the mockup's look, use it instead of
recreating the mockup's bespoke class from scratch:

- **Buttons**: `icon-btn`/`icon-btn.save`/`icon-btn.revert`/`icon-btn.danger` become
  Bootstrap's `.btn.btn-sm.btn-icon`-style combinations (`.btn-outline-dark`,
  `.btn-outline-secondary`, `.btn-outline-danger` -- already used today in
  `Programs.razor` and already themed by `app.css`'s black-primary override) sized down
  and laid out with utility classes (`d-inline-flex`, `align-items-center`, `gap-*`,
  `rounded`), not a hand-rolled `.icon-btn` class reimplementing what `.btn-sm` already
  does.
- **Layout**: the navbar/sidebar/main three-region shell keeps using Bootstrap's grid
  (`container-fluid`/`row`/`col-*`) and existing `navbar`/`sidebar` scaffolding already
  in `MainLayout.razor`/`NavMenu.razor` (per `docs/ui-decisions.md`'s "#32" -- the
  Bootstrap dashboard example this app is already built on) rather than the mockup's
  flexbox `.shell`/`.main` divs. Only the parts Bootstrap's dashboard example doesn't
  already give us -- the fixed 240px width (vs. the responsive `col-md-3 col-lg-2`),
  the brand block's dark inset panel, tile-grid cards -- get new rules, and those should
  lean on Bootstrap utility classes (`d-flex`, `gap-3`, `rounded`, `border`,
  `bg-body-tertiary`) wherever one exists, with custom CSS reserved for what utilities
  can't express (depth-indent steps, chevron rotation, the sheet's border/tint scheme).
- **Cards/tiles**: the Home page's tile grid uses Bootstrap's `.card`/`.card-body`
  (`row row-cols-*`/`g-*` for the grid) instead of the mockup's bespoke `.tile`/
  `.card-grid`, styled to match via `app.css` overrides the same way `.btn-primary`
  already is.
- **Table**: the spreadsheet keeps Bootstrap's `.table` as its base (`table-borderless`
  or a custom border override, not a fully custom `table.sheet` reset) -- the depth-tint
  background (`row-depth-0`), indentation, and chevron are the only genuinely new rules,
  since Bootstrap has no concept of nested/collapsible table rows.

**Alternative considered**: copy the mockup's `<style>` block into `app.css` near-verbatim
and reproduce its class names 1:1. Rejected -- the mockup was built in an isolated canvas
sandbox with no Bootstrap loaded (its README notes it can't even load Bootstrap Icons'
CDN font, hence the hand-drawn SVGs), so its CSS reinvents things Bootstrap already
provides (buttons, cards, grid, borders, spacing scale). Copying it wholesale would leave
two parallel styling systems in `app.css` -- Bootstrap's utilities (already used
throughout `Programs.razor` today) and a second, redundant bespoke system -- which is
exactly the kind of drift `CLAUDE-blazor-ui.md`'s "prefer the CSS framework's existing
utility classes... over new custom CSS" styling rule exists to prevent. Task 4.5 (and 1.3,
3.2) should be read as "achieve this visual result primarily with Bootstrap," not "copy
these CSS rules."

## Risks / Trade-offs

- **[Risk]** Moving `Programs.razor` out of `Pages/Admin/` could break IDE/test-project
  references that hardcode the old namespace. **Mitigation**: grep
  `Trainfree.Admin.Admin` across the solution (including `Trainfree.Admin.Tests`) before
  merging; update every reference in the same PR.
- **[Risk]** `/admin` → `/programs` is a breaking route change with no redirect.
  **Mitigation**: acceptable -- single-user, not yet linked from anywhere external, no
  bookmarks worth preserving at this stage of the project. Explicitly not adding a
  redirect stub to avoid carrying dead-route cruft into a fresh app.
- **[Risk]** Restyling `VersionIndicator` from the shared `Trainfree.Versioning` project
  touches code the not-yet-built `Trainfree.Workout` will also consume.
  **Mitigation**: the component's own markup/logic is untouched; only `Trainfree.Admin`'s
  `app.css` gains new class-based styling rules. `Trainfree.Workout` is free to define
  its own `.version-pill` styling (or none) when it's built in slice 8 -- nothing here
  binds it to Admin's navy/green palette.
- **[Trade-off]** Home page tiles for Categories/Exercises are visually present but
  inert (no link, no live count) until slices 5-6. This is deliberate per the roadmap's
  slice 4 description, not an oversight -- flagging it here so it isn't "fixed" by
  accident mid-slice.

## Migration Plan

No data migration. Deploy is the existing tag-per-slice flow (`CLAUDE.md`): merge to
`main`, push a `v0.0.N` tag. Purely a client-side static-asset change; no coordinated
Worker deploy required, though the existing deploy pipeline still stamps and redeploys
both together as usual.

## Open Questions

None outstanding -- the five behavioral questions this design depended on were resolved
during `/opsx:explore` before this document was written.
