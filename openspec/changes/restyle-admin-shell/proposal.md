## Why

`Trainfree.Admin` still wears the stock Blazor/Bootstrap dashboard scaffold: an unstyled
`<table>` for programs/sessions, a generic "Trainfree" brand with an "Admin" wrapper link,
and a `Home` page that is a single stub line. `docs/design/admin-mockups/` (hi-fi, already
reviewed) defines the real v0.1 look -- a bordered, depth-indented spreadsheet with
chevron-collapsible rows, a fixed sidebar, and a tile-based Home page -- but nothing in the
codebase implements it yet. Slice 3 (`add-sessions-crud`) gave the admin page a real
two-level hierarchy (Program -> Session) to restyle against; this slice is purely that
restyle, so slices 5-7 (Categories, Exercises, the full spreadsheet) build on the right
visual foundation instead of retrofitting it later.

## What Changes

- Restyle the navbar: fixed 240px dark brand block reading "Trainfree Admin" with an
  inline-SVG dumbbell mark (no `bi-*` equivalent exists), version pill
  (`VersionIndicator`) in the top-right, single location only.
- Restyle the sidebar to a fixed 240px `Home` / `Programs` list (no `Admin` wrapper link --
  **BREAKING** for anyone with `/admin` bookmarked, see route rename below). Final
  `Home` / `Categories` / `Exercises` / `Programs` order lands across slices 5-6; this
  slice ships only the two links that have pages behind them.
- Rewrite `Home.razor` from its one-line stub into the tile-grid landing page from
  `Home.dc.html`: a `Programs` tile that links to `/programs`, plus `Categories` and
  `Exercises` tiles rendered visually but inert (no route yet -- those ship in slices 5-6).
- Rename the Programs admin route from `/admin` to `/programs`. **BREAKING**: `/admin` no
  longer resolves to this page.
- Restyle the Programs page's table into the bordered spreadsheet look (`sheet-wrap`,
  depth-indented rows, `icon-btn`-style Save/Revert/Delete) without changing the
  underlying interaction model -- rows keep the existing working/saved-value,
  `IsDirty`-gated Save+Revert pattern; only its visual presentation changes.
- Add per-row expand/collapse state for each program's sessions, with the mockup's
  chevron treatment. This is new client-side state (not present today, where every
  session always renders).
- No changes to the Worker API, D1 schema, or any request/response shape.

## Capabilities

### New Capabilities
- `admin-shell`: the navbar, sidebar, Home landing page, and routing/navigation
  structure shared by every admin page (brand, version indicator placement, nav links,
  tile grid).

### Modified Capabilities
- `programs`: the "Admin program list UI" requirement's visual presentation changes
  (bordered spreadsheet, `icon-btn` styling) and its route changes from `/admin` to
  `/programs`; the working/saved-value Save/Revert behavior itself is unchanged.
- `sessions`: the "Admin session rows nested under their program" requirement gains
  per-program expand/collapse behavior (chevron-driven, new state); everything else
  about session row behavior (create/rename/delete, Save/Revert, error handling) is
  unchanged.

## Impact

- `src/Trainfree.Admin/Layout/MainLayout.razor`, `NavMenu.razor` -- restyled chrome,
  removed `Admin` link.
- `src/Trainfree.Admin/Pages/Home.razor` -- rewritten from a stub into the tile grid.
- `src/Trainfree.Admin/Pages/Admin/Programs.razor` -- route rename, restyle, expand/
  collapse state. (File path/namespace may move out of the `Admin/` subfolder now that
  `/admin` no longer exists as a concept -- see design.md.)
- `src/Trainfree.Admin/wwwroot/css/app.css` -- new rules for the sheet/navbar/sidebar/
  tile styling ported from the mockups.
- `src/Trainfree.Versioning/VersionIndicator.razor` -- shared with the future
  `Trainfree.Workout` app; restyled to the pill look used in the navbar.
- No Worker (`Trainfree.AdminApi`), D1 migration, or shared-domain (`Trainfree.Domain`)
  changes.
- Docs: `docs/design/admin-mockups/Home.dc.html`, `docs/design/admin-mockups/README.md`,
  `CLAUDE-blazor-ui.md`, `docs/ui-decisions.md` (see tasks.md).
