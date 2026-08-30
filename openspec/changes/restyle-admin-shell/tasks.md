## 1. Navbar and sidebar shell

- [ ] 1.1 Restyle `MainLayout.razor`'s navbar: fixed 240px dark brand block reading
      "Trainfree Admin" with an inline-SVG dumbbell mark, `VersionIndicator` in the
      top-right only.
- [ ] 1.2 Restyle `NavMenu.razor`'s sidebar to a fixed 240px width; remove the `Admin`
      `NavLink` entirely, leaving `Home` and `Programs` (pointing at the new `/programs`
      route from task group 3).
- [ ] 1.3 Add the navbar/sidebar CSS overrides needed to match `Home.dc.html`'s look
      (fixed 240px width, brand's dark inset panel) to `app.css`, building on Bootstrap's
      existing `navbar`/`bg-body-tertiary` sidebar classes already in `MainLayout.razor`/
      `NavMenu.razor` -- not a wholesale copy of the mockup's `.navbar`/`.sidebar`/
      `.nav-link` rules (design.md decision 7).
- [ ] 1.4 Update/add bUnit tests for `MainLayout`/`NavMenu` covering: sidebar shows only
      `Home` and `Programs`, no `Admin` link exists, active-link highlighting.
- [ ] 1.5 With the local dev stack running (`wrangler dev` on port 9999 for
      `Trainfree.AdminApi`, `dotnet run` for `Trainfree.Admin` at `localhost:5280`), use
      `claude-in-chrome` to load the app and screenshot the navbar/sidebar against
      `Home.dc.html`/`EmptyState.dc.html`: fixed-width brand block with the dumbbell
      mark, version pill top-right, sidebar showing only `Home`/`Programs` with active-
      link highlighting. Check the browser console for errors.

## 2. Version indicator restyle

- [ ] 2.1 Restyle `VersionIndicator.razor`'s `.version-stamp` output to the
      `.version-pill` look (check icon + version text) via `Trainfree.Admin`'s
      `app.css` -- no change to the component's markup structure or `Trainfree.Versioning`
      itself, per design.md decision 5.
- [ ] 2.2 Confirm (test or manual check) the version indicator renders exactly once, in
      the navbar, on every admin page including Home.
- [ ] 2.3 Via `claude-in-chrome`, screenshot the navbar pill against `Home.dc.html`'s
      `.version-pill` and confirm no second version display exists anywhere in the Home
      page body.

## 3. Home landing page

- [ ] 3.1 Rewrite `Pages/Home.razor` from its one-line stub into the tile-grid layout
      from `Home.dc.html`: `Programs` tile with a live `NavLink` to `/programs`,
      `Categories` and `Exercises` tiles rendered in the same visual style with no link
      (per design.md decision 4 -- no dead route).
- [ ] 3.2 Style the tiles using Bootstrap's `.card`/`.card-body` (grid via
      `row row-cols-* g-*`) with `app.css` overrides to match the mockup's look, rather
      than a bespoke `.card-grid`/`.tile` class set (design.md decision 7).
- [ ] 3.3 Add bUnit tests: Programs tile navigates to `/programs` on click; Categories
      and Exercises tiles render but are not clickable/navigable.
- [ ] 3.4 Via `claude-in-chrome`, load `/` and screenshot the tile grid against
      `Home.dc.html`; click the Programs tile and confirm it navigates to `/programs`;
      confirm Categories/Exercises tiles are visually present but not clickable.

## 4. Programs page: move, route rename, restyle

- [ ] 4.1 Move `Pages/Admin/Programs.razor` to `Pages/Programs.razor`; update its
      `@page` directive from `/admin` to `/programs` and its namespace/`@using`
      references (`Trainfree.Admin.Admin` -> whatever the new folder resolves to per
      IDE0130).
- [ ] 4.2 Grep the solution (including `Trainfree.Admin.Tests`) for
      `Trainfree.Admin.Admin` / references to the old file path and update every hit in
      this same PR (design.md risk 1).
- [ ] 4.3 Restyle the programs table to match `Main.dc.html`'s bordered spreadsheet look,
      keeping Bootstrap's `.table` as the base (border/tint overrides in `app.css`, not a
      full custom `table.sheet` reset), using only the `Name` column (no Type/Reps/
      Weight/Sets/Rest/Side/Note columns -- those arrive in slice 7).
- [ ] 4.4 Restyle program and session row action buttons (`Save`/`Revert`/`Delete`) using
      Bootstrap's existing `.btn.btn-sm.btn-outline-*` classes (already used today) sized
      and laid out with utility classes to match the mockup's icon-button look, rather
      than a bespoke `.icon-btn` class set. Preserve all existing `data-testid` attributes
      unchanged.
- [ ] 4.5 Add the row-depth/indent/border CSS to `app.css` needed to match `Main.dc.html`'s
      look (pared down to the Program/Session depth levels this slice has) -- only the
      parts Bootstrap's table/utility classes don't already cover (design.md decision 7).
- [ ] 4.6 Verify existing Programs/Sessions bUnit tests still pass unmodified against
      the restyled markup (should be a pure presentation swap per design.md decision 1)
      -- fix any that accidentally depended on removed Bootstrap classes rather than
      `data-testid`.
- [ ] 4.7 Via `claude-in-chrome`, load `/programs` and screenshot the sheet against
      `Main.dc.html`'s Program/Session rows; click through add program, edit-and-save,
      edit-and-revert, and delete, confirming the `icon-btn`-style Save/Revert/Delete
      controls appear/disappear correctly and no console errors are logged.

## 5. Session expand/collapse

- [ ] 5.1 Add `HashSet<ProgramId> _collapsedIds` (or equivalent) to `Programs.razor`;
      default empty so every program starts expanded.
- [ ] 5.2 Add a chevron control to each program row that toggles that program's
      membership in `_collapsedIds`; render session rows (and the program's
      `Add Session` row) conditionally on the program being expanded.
- [ ] 5.3 Add the chevron open/closed CSS treatment (rotation) from `Main.dc.html`.
- [ ] 5.4 Add bUnit tests for the four expand/collapse scenarios in
      `specs/sessions/spec.md`: starts expanded, collapsing hides sessions, expanding
      restores them without a re-fetch, collapsing one program leaves others unaffected.
- [ ] 5.5 Via `claude-in-chrome`, click a program's chevron on `/programs` and screenshot
      the collapsed state (sessions hidden, chevron rotated) against `Main.dc.html`'s
      collapsed rows (e.g. "Workout B"); expand it again and confirm sessions reappear
      with no network request fired (check `read_network_requests`).

## 6. Documentation updates

- [ ] 6.1 Edit `docs/design/admin-mockups/Home.dc.html` to remove the redundant
      bottom-of-page `.meta-row` version block (navbar pill is the only instance, per
      design.md decision 5).
- [ ] 6.2 Edit `docs/design/admin-mockups/README.md` to note the navbar brand icon as
      the deliberate inline-SVG exception to the "implementation should keep using
      `bi-*`" guidance.
- [ ] 6.3 Edit `CLAUDE-blazor-ui.md`'s Icons section to add the same carve-out
      durably: an icon-font-less brand/wordmark mark may stay inline SVG.
- [ ] 6.4 Add a new dated entry to `docs/ui-decisions.md` documenting: the sheet-style
      chrome restyle, chevron collapse as new client state, the `/admin` -> `/programs`
      rename, `Admin` NavLink removal, the `bi-*` brand-icon exception, and the version
      indicator staying single-location. Include a line noting the mockups were matched
      visually using Bootstrap components/utilities rather than porting their CSS
      verbatim (design.md decision 7) -- flagged as a forward-looking note for whoever
      implements slices 5-7, since those mockups came from the same Bootstrap-less
      canvas sandbox and invite the same copy-paste temptation. Add a one-line
      forward-pointer on the existing "#32" entry noting its "Home and Admin" sidebar
      description is now superseded, without rewriting #32 itself.

## 7. Verification

- [ ] 7.1 Run the full `Trainfree.Admin.Tests` suite; all green.
- [ ] 7.2 Run CSharpier format check.
- [ ] 7.3 Via `claude-in-chrome`, walk the full flow end to end in one pass (Home ->
      Programs tile -> add/rename/revert/delete a program and a session -> expand/
      collapse -> back to Home) to catch anything that only breaks when the groups are
      combined, not visible in any single group's isolated check.
