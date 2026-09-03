## Why

The shipped `Category` entity ("Warm Up", "A", "B", ...) names a sequential block within
a session's workout flow, not a taxonomy tag. "Category" reads as classification
(chest/legs/cardio); "Phase" better matches what the entity actually models -- see
`docs/trainfree-proposal.md`'s own framing of these as "sections" of a session. Renaming
now, before slice 7 (`add-program-categories-exercises-crud`) introduces the
`SessionCategory` join, is the cheapest point to do it: per the current spec, nothing
references `categories` yet and delete is unconditional.

## What Changes

- Rename the `Category` entity to `Phase` across the domain, Worker API, and Admin UI:
  `CategoryId` -> `PhaseId`, `categories` D1 table -> `phases` (new migration, not an
  edit to the old one), `/api/categories` routes -> `/api/phases`, `Categories.razor` ->
  `Phases.razor`, and all supporting types (`CategorySummary`, `CreateCategoryOutcome`,
  `RenameCategoryOutcome`, `DeleteCategoryOutcome`, `ICategoriesApiClient` /
  `CategoriesApiClient`) renamed to their `Phase` equivalents.
- `CAT-` surrogate-key prefix becomes `PHS-` (still 6 Crockford base32 characters after
  the prefix, matching the existing 3-char-prefix convention used by `PRG-` and `SNN-`).
- Sidebar nav label "Categories" becomes "Phases"; Home page tile updates to match.
- Update all doc and mockup references (`docs/trainfree-roadmap.md`,
  `docs/trainfree-proposal.md`, `docs/screen-mockups.md`,
  `docs/design/admin-mockups/Categories.dc.html`,
  `docs/design/admin-mockups/CategoriesEmpty.dc.html`, `Main.dc.html`, `Home.dc.html`,
  `README.md`, `canvas.json`) to say "Phase"/"Phases" instead of "Category"/"Categories".
- **BREAKING**: the `/api/categories` routes are removed and replaced by
  `phases` / `/api/phases`. Existing `categories` rows are copied into `phases` by a data
  migration; the `categories` table itself is kept for now and dropped in a later slice,
  once the new Worker is confirmed live.
- No behavior change: name-length bounds (4-100 chars), case-insensitive uniqueness,
  creation-order listing, and unconditional delete all carry over unchanged under the new
  name.

## Capabilities

### New Capabilities
- `phases`: canonical phase library (rename of the `categories` capability) -- ID format,
  CRUD API, name validation/uniqueness, and the Blazor admin UI, all under the `Phase`
  name.

### Modified Capabilities
- `categories`: capability removed and superseded by `phases`; this spec file is deleted
  as part of the change.

## Impact

- **Domain**: `Trainfree.Domain/Ids/CategoryId.cs` -> `PhaseId.cs`.
- **Worker** (`Trainfree.AdminApi`): `src/categories.js` -> `phases.js`, route
  registration in `src/index.js`, ID generation in `src/ids.js`, validation in
  `src/validation.js`, plus their `.test.js` files.
- **D1 schema**: new migrations adding `phases` and copying existing `categories` rows
  into it; `categories` itself is dropped in a later slice, once the new Worker is
  confirmed live.
- **Admin client** (`Trainfree.Admin`): `Admin/CategoriesApiClient.cs`,
  `Admin/ICategoriesApiClient.cs`, `Admin/CategorySummary.cs`,
  `Admin/CreateCategoryOutcome.cs`, `Admin/RenameCategoryOutcome.cs`,
  `Admin/DeleteCategoryOutcome.cs`, `Pages/Categories.razor`, sidebar nav component.
- **Tests**: `Trainfree.Domain.Tests/Ids/CategoryIdTests.cs`,
  `Trainfree.Admin.Tests/Admin/CategoriesPageTests.cs`,
  `Trainfree.Admin.Tests/Admin/CategoriesApiClientTests.cs`, Worker `ids.test.js`,
  `validation.test.js`, `index.test.js`.
- **OpenSpec**: `openspec/specs/categories/spec.md` removed, replaced by
  `openspec/specs/phases/spec.md`.
- **Docs**: roadmap, proposal, screen-mockups, and the admin-mockups design files listed
  above.
- No impact to `Trainfree.Workout`/`Trainfree.WorkoutApi` (not yet built) or to any other
  shipped capability (`programs`, `sessions`) beyond the nav label.
