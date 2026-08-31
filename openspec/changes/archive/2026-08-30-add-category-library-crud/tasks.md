## 1. D1 schema

- [x] 1.1 Add migration `000N_create_categories.sql` creating `categories`
      (`id`, `category_id`, `name`, `created_at`, `updated_at`), mirroring `programs`'
      column shape from `0001_create_programs.sql`.
- [x] 1.2 Add migration `000N_add_categories_name_unique_index.sql` creating a
      case-insensitive unique index on `categories.name`, mirroring
      `0002_add_programs_name_unique_index.sql`.

## 2. Worker: category id and validation

- [x] 2.1 Add `CATEGORY_PREFIX = "CAT-"` and `generateCategoryId`/`isValidCategoryId` to
      `ids.js`, following `generateProgramId`/`isValidProgramId`.
- [x] 2.2 Add a test in `ids.test.js` for the new id generator/validator pair.
- [x] 2.3 Add `validateCategoryName` to `validation.js` (delegates to `validateName`,
      same as `validateProgramName`).
- [x] 2.4 Add a test in `validation.test.js` for `validateCategoryName`.

## 3. Worker: categories module (red-green-refactor)

- [x] 3.1 Write failing tests for `listCategories`, `createCategory`,
      `renameCategory`, `deleteCategory`, covering the scenarios in
      `specs/categories/spec.md` (empty list, creation order, duplicate name on
      create/rename case-insensitively, rename-to-own-name succeeds, length bounds,
      not-found on rename/delete, unconditional delete). Written as `index.test.js`
      route-level tests (`GET/POST/PATCH/DELETE /api/categories`), not a standalone
      `categories.test.js` -- matching this codebase's existing convention, where
      `programs.js`/`sessions.js` also have no dedicated unit test file and are
      exercised only through `index.test.js`'s `SELF.fetch` integration tests.
- [x] 3.2 Implement `src/Trainfree.AdminApi/src/categories.js`
      (`listCategories`, `createCategory`, `renameCategory`, `deleteCategory`), mirroring
      `programs.js` -- no parent-scoping, no usage guard on delete.
- [x] 3.3 Confirm all tests from 3.1 pass.

## 4. Worker: routes

- [x] 4.1 Add `handleCategoriesCollection` (GET/POST) and `handleCategoryResource`
      (PATCH/DELETE) to `index.js`, mirroring `handleProgramsCollection`/
      `handleProgramResource`.
- [x] 4.2 Wire `/api/categories` and `/api/categories/:id` into the router's path-segment
      dispatch.
- [x] 4.3 Add route-level tests to `index.test.js` covering the full CRUD flow through
      `/api/categories`, including CORS/OPTIONS handling already exercised for
      `/api/programs`.

## 5. Admin client: API client and outcome types

- [x] 5.1 Add `CategorySummary.cs` (id, name), mirroring `ProgramSummary.cs`.
- [x] 5.2 Add `CreateCategoryOutcome.cs`, `RenameCategoryOutcome.cs`,
      `DeleteCategoryOutcome.cs` (success/failure discriminated types), mirroring the
      `*ProgramOutcome.cs` files.
- [x] 5.3 Add `ICategoriesApiClient.cs` and `CategoriesApiClient.cs` +
      `CategoriesApiClient.Logging.cs`, mirroring `IProgramsApiClient.cs`/
      `ProgramsApiClient.cs` (list, create, rename, delete against `/api/categories`).
- [x] 5.4 Register `ICategoriesApiClient` in DI (wherever `IProgramsApiClient` is
      registered).
- [x] 5.5 (Not originally planned) Added `CategoriesApiClientTests.cs`, mirroring the
      existing `ProgramsApiClientTests.cs`, in `tests/Trainfree.Admin.Tests` -- a bUnit/
      xUnit test project already existed in the repo (`ProgramsApiClientTests`,
      `ProgramsPageTests`, `NavMenuTests`, `HomeTests`) that this proposal's design.md
      didn't account for.

## 6. Admin client: Categories page

- [x] 6.1 Add `Categories.razor` at `/categories`: loads via `GET /api/categories` on
      init, renders `CategoriesEmpty.dc.html`'s empty state when the list is empty, and
      a flat row-per-category table (name column + row actions only, no "Used in"
      column) otherwise -- following `Programs.razor`'s working/saved-value dirty-row
      pattern (`CategoryRow` inner class: `Id`, `Name`, `SavedName`, `IsDirty`, `Error`).
- [x] 6.2 Wire `Add Category`, per-row `Save`/`Revert`, and per-row `Delete` to the
      client-side length-bound check (4-100 chars) then the API client, surfacing
      `400`/`409` outcomes as a row-level error message without throwing.
- [x] 6.3 Surface a page-level load-failed message on `GET /api/categories` failure,
      matching `Programs.razor`'s `OnInitializedAsync` catch pattern.
- [x] 6.4 (Not originally planned) Added `CategoriesPageTests.cs`, mirroring
      `ProgramsPageTests.cs`, and updated `NavMenuTests.cs`/`HomeTests.cs` for the new
      nav link and the now-live `Home` tile.

## 7. Admin shell wiring

- [x] 7.1 Add a `Categories` `NavLink` to `NavMenu.razor` between `Home` and `Programs`.
- [x] 7.2 Turn `Home.razor`'s disabled `Categories` tile into a live `NavLink` to
      `/categories`, mirroring the `Programs` tile.

## 8. Verification

- [x] 8.1 Run the Worker's vitest suite (`npm test` in `Trainfree.AdminApi`) -- all
      green (164 tests).
- [x] 8.2 Run `dotnet build` for the solution -- clean, no new warnings; `dotnet test`
      also run -- all green (36 + 22 + 94 tests).
- [x] 8.3 Manually ran the app locally (`wrangler dev` on 9999 + Blazor dev server on
      5280, after applying the new migrations to the local D1 with
      `npm run db:migrate:local`) and exercised the Categories page end to end via
      browser automation: empty state renders with the `Add Category` CTA; add creates
      and focuses a new row; renaming a second row to "warm up" against an existing
      "Warm Up" surfaces the server's 409 (`A category named "warm up" already
      exists.`) as a row-level error without crashing; Revert restores the saved name
      and clears the error; Delete removes the row; deleting the last row returns to
      the empty state. Also confirmed via the running app: sidebar shows `Home` /
      `Categories` / `Programs` with `Categories` active on `/categories`, and the
      `Home` page's `Categories` tile navigates to `/categories`. Both dev servers
      stopped afterward.
