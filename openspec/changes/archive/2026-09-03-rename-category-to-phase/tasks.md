## 1. D1 schema

- [x] 1.1 Add `0006_create_phases.sql`: `CREATE TABLE phases` mirroring the `categories`
      shape (`id`, `phase_id TEXT NOT NULL UNIQUE`, `name`, `created_at`, `updated_at`).
- [x] 1.2 Add `0007_add_phases_name_unique_index.sql`: `CREATE UNIQUE INDEX
      idx_phases_name_nocase ON phases (name COLLATE NOCASE)`.
- [x] 1.3 Add `0008_copy_categories_to_phases.sql`: copy existing `categories` rows into
      `phases` (prefix rewrite); `categories` is not dropped in this change.

## 2. Worker (`Trainfree.AdminApi`)

- [x] 2.1 Rename `src/categories.js` -> `src/phases.js`; update all `categories` table
      references to `phases`, `category_id` column to `phase_id`, and internal
      identifiers/variable names from `category`/`categories` to `phase`/`phases`.
- [x] 2.2 Rename `src/categories.test.js` -> `src/phases.test.js` (if separate from
      `index.test.js`); update accordingly.
- [x] 2.3 In `src/ids.js`, change the category ID prefix constant from `CAT-` to `PHS-`
      (3-char prefix, matching `PRG-`/`SNN-`); update `src/ids.test.js`.
- [x] 2.4 In `src/validation.js`, rename category-specific validation exports/messages to
      phase-flavored names; update `src/validation.test.js`.
- [x] 2.5 In `src/index.js`, replace `/api/categories` route registrations with
      `/api/phases`; update `src/index.test.js` route assertions.
- [x] 2.6 Run `npx vitest run` in `Trainfree.AdminApi` and confirm all tests pass.

## 3. Domain (`Trainfree.Domain`)

- [x] 3.1 Rename `Ids/CategoryId.cs` -> `Ids/PhaseId.cs`; rename the type and its `PHS-`
      prefix constant to match task 2.3's Worker-side prefix.
- [x] 3.2 Rename `Trainfree.Domain.Tests/Ids/CategoryIdTests.cs` ->
      `Ids/PhaseIdTests.cs`; update the type name and prefix assertions.

## 4. Admin client (`Trainfree.Admin`)

- [x] 4.1 Rename `Admin/CategorySummary.cs` -> `Admin/PhaseSummary.cs`.
- [x] 4.2 Rename `Admin/CreateCategoryOutcome.cs` -> `Admin/CreatePhaseOutcome.cs`,
      `Admin/RenameCategoryOutcome.cs` -> `Admin/RenamePhaseOutcome.cs`,
      `Admin/DeleteCategoryOutcome.cs` -> `Admin/DeletePhaseOutcome.cs`.
- [x] 4.3 Rename `Admin/ICategoriesApiClient.cs` -> `Admin/IPhasesApiClient.cs` and
      `Admin/CategoriesApiClient.cs` -> `Admin/PhasesApiClient.cs`; update the `/api/`
      base route and all type references.
- [x] 4.4 Rename `Pages/Categories.razor` -> `Pages/Phases.razor`; update the `@page`
      route to `/phases`, page title, `Add Category`/`Add Phase` and other UI copy, and
      `data-testid` attributes.
- [x] 4.5 Update the sidebar nav component: label "Categories" -> "Phases", route
      `/categories` -> `/phases`, keeping its position in the nav order.
- [x] 4.6 Update the Home page's Categories tile: label and route to Phases/`/phases`.
- [x] 4.7 Register `IPhasesApiClient`/`PhasesApiClient` in DI setup (`Program.cs` or
      equivalent) in place of the categories registration.

## 5. Admin tests (`Trainfree.Admin.Tests`)

- [x] 5.1 Rename `Admin/CategoriesApiClientTests.cs` -> `Admin/PhasesApiClientTests.cs`;
      update type references and route assertions.
- [x] 5.2 Rename `Admin/CategoriesPageTests.cs` -> `Admin/PhasesPageTests.cs`; update
      type references, route, and UI copy assertions.

## 6. OpenSpec capability

- [x] 6.1 Confirm `specs/categories/spec.md` (REMOVED) and `specs/phases/spec.md`
      (ADDED) in this change directory are complete and consistent with the renamed
      code (already drafted as part of this proposal).

## 7. Docs and mockups

- [x] 7.1 Update `docs/trainfree-roadmap.md`: slice 5's description and slice 7's
      `SessionCategory` reference (-> `SessionPhase`) to use Phase terminology.
- [x] 7.2 Update `docs/trainfree-proposal.md`: the "'Categories' is the correct term..."
      note to reflect "Phase" instead, keeping its "Warm Up, A, B" example.
- [x] 7.3 Update `docs/screen-mockups.md` category references (if any beyond the
      "Warm Up" section labels already used) to Phase terminology.
- [x] 7.4 Rename `docs/design/admin-mockups/Categories.dc.html` ->
      `Phases.dc.html` and `CategoriesEmpty.dc.html` -> `PhasesEmpty.dc.html`; update
      their internal copy ("category" -> "phase", "Categories" -> "Phases").
- [x] 7.5 Update `docs/design/admin-mockups/Main.dc.html` and `Home.dc.html`: category
      picker copy, "+ Add Category" labels, and comments referencing "category" ->
      "phase".
- [x] 7.6 Update `docs/design/admin-mockups/README.md` and `canvas.json` references to
      the renamed mockup files and copy.

## 8. Verification

- [x] 8.1 `dotnet build` the full solution with no warnings introduced by the rename.
- [x] 8.2 `dotnet csharpier format` (or `check`) clean.
- [x] 8.3 Full `dotnet test` run (unit + bUnit) green.
- [x] 8.4 Worker `npx vitest run` green (already covered by 2.6, re-run as final check).
- [x] 8.5 `openspec validate rename-category-to-phase --strict` passes.
- [x] 8.6 Grep the repo for residual case-insensitive `categor` outside
      `openspec/changes/*/archive` history and this change's own delta files
      (`specs/categories/spec.md` is expected to remain as the REMOVED-requirements
      delta) to confirm nothing was missed.
