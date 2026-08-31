## 1. D1 schema

- [ ] 1.1 Add migration `000N_create_categories.sql` creating `categories`
      (`id`, `category_id`, `name`, `created_at`, `updated_at`), mirroring `programs`'
      column shape from `0001_create_programs.sql`.
- [ ] 1.2 Add migration `000N_add_categories_name_unique_index.sql` creating a
      case-insensitive unique index on `categories.name`, mirroring
      `0002_add_programs_name_unique_index.sql`.

## 2. Worker: category id and validation

- [ ] 2.1 Add `CATEGORY_PREFIX = "CAT-"` and `generateCategoryId`/`isValidCategoryId` to
      `ids.js`, following `generateProgramId`/`isValidProgramId`.
- [ ] 2.2 Add a test in `ids.test.js` for the new id generator/validator pair.
- [ ] 2.3 Add `validateCategoryName` to `validation.js` (delegates to `validateName`,
      same as `validateProgramName`).
- [ ] 2.4 Add a test in `validation.test.js` for `validateCategoryName`.

## 3. Worker: categories module (red-green-refactor)

- [ ] 3.1 Write failing tests for `listCategories`, `createCategory`,
      `renameCategory`, `deleteCategory` in a new `categories.test.js`, covering the
      scenarios in `specs/categories/spec.md` (empty list, creation order, duplicate
      name on create/rename case-insensitively, rename-to-own-name succeeds, length
      bounds, not-found on rename/delete, unconditional delete).
- [ ] 3.2 Implement `src/Trainfree.AdminApi/src/categories.js`
      (`listCategories`, `createCategory`, `renameCategory`, `deleteCategory`), mirroring
      `programs.js` -- no parent-scoping, no usage guard on delete.
- [ ] 3.3 Confirm all tests from 3.1 pass.

## 4. Worker: routes

- [ ] 4.1 Add `handleCategoriesCollection` (GET/POST) and `handleCategoryResource`
      (PATCH/DELETE) to `index.js`, mirroring `handleProgramsCollection`/
      `handleProgramResource`.
- [ ] 4.2 Wire `/api/categories` and `/api/categories/:id` into the router's path-segment
      dispatch.
- [ ] 4.3 Add route-level tests to `index.test.js` covering the full CRUD flow through
      `/api/categories`, including CORS/OPTIONS handling already exercised for
      `/api/programs`.

## 5. Admin client: API client and outcome types

- [ ] 5.1 Add `CategorySummary.cs` (id, name), mirroring `ProgramSummary.cs`.
- [ ] 5.2 Add `CreateCategoryOutcome.cs`, `RenameCategoryOutcome.cs`,
      `DeleteCategoryOutcome.cs` (success/failure discriminated types), mirroring the
      `*ProgramOutcome.cs` files.
- [ ] 5.3 Add `ICategoriesApiClient.cs` and `CategoriesApiClient.cs` +
      `CategoriesApiClient.Logging.cs`, mirroring `IProgramsApiClient.cs`/
      `ProgramsApiClient.cs` (list, create, rename, delete against `/api/categories`).
- [ ] 5.4 Register `ICategoriesApiClient` in DI (wherever `IProgramsApiClient` is
      registered).

## 6. Admin client: Categories page

- [ ] 6.1 Add `Categories.razor` at `/categories`: loads via `GET /api/categories` on
      init, renders `CategoriesEmpty.dc.html`'s empty state when the list is empty, and
      a flat row-per-category table (name column + row actions only, no "Used in"
      column) otherwise -- following `Programs.razor`'s working/saved-value dirty-row
      pattern (`CategoryRow` inner class: `Id`, `Name`, `SavedName`, `IsDirty`, `Error`).
- [ ] 6.2 Wire `Add Category`, per-row `Save`/`Revert`, and per-row `Delete` to the
      client-side length-bound check (4-100 chars) then the API client, surfacing
      `400`/`409` outcomes as a row-level error message without throwing.
- [ ] 6.3 Surface a page-level load-failed message on `GET /api/categories` failure,
      matching `Programs.razor`'s `OnInitializedAsync` catch pattern.

## 7. Admin shell wiring

- [ ] 7.1 Add a `Categories` `NavLink` to `NavMenu.razor` between `Home` and `Programs`.
- [ ] 7.2 Turn `Home.razor`'s disabled `Categories` tile into a live `NavLink` to
      `/categories`, mirroring the `Programs` tile.

## 8. Verification

- [ ] 8.1 Run the Worker's vitest suite (`npm test` in `Trainfree.AdminApi`) -- all
      green.
- [ ] 8.2 Run `dotnet build` for the solution -- clean, no warnings.
- [ ] 8.3 Manually run the app locally (`wrangler dev` + Blazor dev server) and exercise
      the Categories page: empty state, add, rename with a duplicate name (expect
      row-level 409 error), delete.
