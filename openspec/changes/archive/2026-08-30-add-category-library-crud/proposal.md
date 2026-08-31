## Why

Sessions currently have no way to be tagged with a category ("Warm Up", "A", "B", ...)
without typing free text, and there's no canonical place those names live. Slice 7
(`add-program-categories-exercises-crud`) needs a `Category` library to pick from before
it can build the category picker on session rows. This slice builds that library on its
own, per the roadmap's slice 5, so slice 7 isn't blocked on it later.

## What Changes

- New `categories` D1 table (`category_id`, `name`, `created_at`, `updated_at`), with a
  case-insensitive unique index on `name`, following the same shape as `programs`.
- New Worker routes: `GET/POST/PATCH/DELETE /api/categories`, mirroring the existing
  `/api/programs` handlers (list, create, rename, delete) -- flat, not nested under
  another resource.
- New `Categories` Blazor admin page at `/categories`, styled per
  `docs/design/admin-mockups/CategoriesEmpty.dc.html` and the populated list state of
  `Categories.dc.html` (name column and row actions only -- see Impact for what's
  excluded).
- `NavMenu.razor` gains a `Categories` link between `Home` and `Programs`.
- `Home.razor`'s existing disabled `Categories` tile becomes a live `NavLink` to
  `/categories`, matching the `Programs` tile's pattern.
- Delete is unconditional in this slice: no session references a category yet (the
  `session_categories` join table doesn't exist until slice 7), so there is nothing to
  block delete against. No "Used in" column, no disabled-delete state -- see design.md
  for why this is a deliberate, not deferred-by-oversight, scope cut from
  `Categories.dc.html`'s fully-populated screen.

## Capabilities

### New Capabilities
- `categories`: Category identifier format, name length/uniqueness rules, the
  `/api/categories` CRUD API, and the Blazor admin page that manages them -- mirroring
  `specs/sessions/spec.md`'s shape but flat (no parent resource) and with unconditional
  delete.

### Modified Capabilities
- `admin-shell`: adds the `Categories` sidebar nav link and activates the `Home` page's
  `Categories` tile as a live link.

## Impact

- New: `src/Trainfree.AdminApi/migrations/0004_create_categories.sql` and
  `0005_add_categories_name_unique_index.sql`, `src/Trainfree.AdminApi/src/categories.js`,
  `src/Trainfree.Admin/Pages/Categories.razor` + `Categories.razor.Logging.cs`,
  `src/Trainfree.Admin/Admin/ICategoriesApiClient.cs` + implementation + outcome types
  + `CategorySummary.cs`, and `src/Trainfree.Domain/Ids/CategoryId.cs`, following the
  `Programs`/`ProgramsApiClient`/`ProgramId` pattern exactly (no session-style nesting).
  Also new: `tests/Trainfree.Admin.Tests/Admin/CategoriesApiClientTests.cs` and
  `CategoriesPageTests.cs`, mirroring the existing `Programs` equivalents in that
  project (found during implementation -- not anticipated when this proposal was
  written).
- Modified: `src/Trainfree.AdminApi/src/index.js` (new `/api/categories` route
  branches), `src/Trainfree.Admin/Layout/NavMenu.razor`, `src/Trainfree.Admin/Pages/Home.razor`,
  `src/Trainfree.Admin/Program.cs` (registers `ICategoriesApiClient` in DI). Also
  modified: `tests/Trainfree.Admin.Tests/Layout/NavMenuTests.cs` and
  `Pages/HomeTests.cs`, updated for the new nav link and the now-live `Home` tile.
- Out of scope: the `session_categories` join table, the category picker on session
  rows, and the usage-guarded delete / "Used in" column from `Categories.dc.html` -- all
  slice 7.
