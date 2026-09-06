## 1. D1 schema

- [x] 1.1 Add migration `0010_create_exercises.sql` creating `exercises`
      (`id`, `exercise_id`, `name`, `created_at`, `updated_at`), mirroring
      `phases`' column shape from `0006_create_phases.sql`. No `type` column.
- [x] 1.2 Add migration `0011_add_exercises_name_unique_index.sql` creating a
      case-insensitive unique index on `exercises.name`, mirroring
      `0007_add_phases_name_unique_index.sql`.

## 2. Worker: exercise id and validation

- [x] 2.1 Add `EXERCISE_PREFIX = "EXR-"` and `generateExerciseId`/
      `isValidExerciseId` to `ids.js`, following `generatePhaseId`/
      `isValidPhaseId`.
- [x] 2.2 Add a test in `ids.test.js` for the new id generator/validator pair.
- [x] 2.3 Add `validateExerciseName` to `validation.js` (delegates to
      `validateName`, same as `validatePhaseName`).
- [x] 2.4 Add a test in `validation.test.js` for `validateExerciseName`.

## 3. Worker: exercises module (red-green-refactor)

- [x] 3.1 Write failing tests in `index.test.js` for the full
      `GET/POST/PATCH/DELETE /api/exercises` route flow, covering the
      scenarios in `specs/exercises/spec.md` (empty list, creation order,
      created_at-tie tiebreak, duplicate name on create/rename
      case-insensitively, rename-to-own-name succeeds, length bounds at and
      outside the 4-100 boundary, not-found on rename/delete, unconditional
      delete) -- matching how `phases`/`programs` are exercised only through
      `index.test.js`'s `SELF.fetch` integration tests, with no standalone
      `exercises.test.js`.
- [x] 3.2 Implement `src/Trainfree.AdminApi/src/exercises.js`
      (`listExercises`, `createExercise`, `renameExercise`,
      `deleteExercise`), mirroring `phases.js` exactly -- no parent-scoping,
      no usage guard on delete, same `LIST_EXERCISES_QUERY`-exported
      creation-order tiebreak pattern.
- [x] 3.3 Confirm all tests from 3.1 pass.

## 4. Worker: routes

- [x] 4.1 Add `handleExercisesCollection` (GET/POST) and
      `handleExerciseResource` (PATCH/DELETE) to `index.js`, mirroring
      `handlePhasesCollection`/`handlePhaseResource`.
- [x] 4.2 Wire `/api/exercises` and `/api/exercises/:id` into the router's
      path-segment dispatch (`routeExercises`, alongside `routePhases`).
- [x] 4.3 Add route-level tests to `index.test.js` covering CORS/OPTIONS
      handling for `/api/exercises`, matching the existing `/api/phases`
      coverage.

## 5. Domain: ExerciseId

- [x] 5.1 Add `ExerciseId.cs` to `Trainfree.Domain/Ids/`, mirroring
      `PhaseId.cs` (`EXR-` prefix, `Parse`/`TryParse` against
      `DomainId.IsValid`).
- [x] 5.2 Add `ExerciseIdTests.cs` to `Trainfree.Domain.Tests/Ids/`,
      mirroring `PhaseIdTests.cs`.

## 6. Admin client: API client and outcome types

- [x] 6.1 Add `ExerciseSummary.cs` (id, name), mirroring `PhaseSummary.cs`.
- [x] 6.2 Add `CreateExerciseOutcome.cs`, `RenameExerciseOutcome.cs`,
      `DeleteExerciseOutcome.cs` (success/failure discriminated types),
      mirroring the `*PhaseOutcome.cs` files.
- [x] 6.3 Add `IExercisesApiClient.cs` and `ExercisesApiClient.cs`, mirroring
      `IPhasesApiClient.cs`/`PhasesApiClient.cs` -- extends `ApiClientBase`,
      uses `ExecuteAsync`/`ReadErrorAsync` for the create/rename/delete
      calls, list call is a plain `GetFromJsonAsync`. No separate
      `ExercisesApiClient.Logging.cs`: `PhasesApiClient.cs` has no
      `[LoggerMessage]` declarations of its own (logging happens in
      `ApiClientBase`), so there is nothing to mirror into a Logging
      partial.
- [x] 6.4 Register `IExercisesApiClient` in `Program.cs` DI, alongside
      `IPhasesApiClient`.
- [x] 6.5 Add `ExercisesApiClientTests.cs` to `tests/Trainfree.Admin.Tests/Admin/`,
      mirroring `PhasesApiClientTests.cs`.

## 7. Admin client: Exercises page

- [x] 7.1 Add `Exercises.razor` at `/exercises`: loads via
      `GET /api/exercises` on init, renders an empty-state view (matching
      `ExercisesEmpty.dc.html`'s copy) when the list is empty, and a flat
      row-per-exercise table (name column + row actions only -- no image,
      no type, no "Used in" column) otherwise, following `Phases.razor`'s
      working/saved-value dirty-row pattern (`ExerciseRow` inner class:
      `Id`, `Name`, `SavedName`, `IsDirty`, `Error`).
- [x] 7.2 Wire `Add Exercise`, per-row `Save`/`Revert`, and per-row `Delete`
      to the client-side length-bound check (4-100 chars) then the API
      client, surfacing `400`/`409` outcomes as a row-level error message
      without throwing.
- [x] 7.3 Surface a page-level load-failed message on `GET /api/exercises`
      failure, matching `Phases.razor`'s `OnInitializedAsync` catch pattern.
- [x] 7.4 Add `Exercises.razor.Logging.cs` for the load-failure
      `[LoggerMessage]`, mirroring `Phases.razor.Logging.cs`.
- [x] 7.5 Add `ExercisesPageTests.cs` to `tests/Trainfree.Admin.Tests/Admin/`,
      mirroring `PhasesPageTests.cs`.

## 8. Admin shell wiring

- [x] 8.1 Add an `Exercises` `NavLink` to `NavMenu.razor` between `Phases`
      and `Programs`, landing the sidebar in its final order (`Home` /
      `Phases` / `Exercises` / `Programs`).
- [x] 8.2 Turn `Home.razor`'s disabled `Exercises` tile (`tile-disabled`,
      `data-testid="home-tile-exercises"`) into a live `NavLink` to
      `/exercises`, mirroring the `Phases`/`Programs` tiles, and update its
      blurb text to drop "type" now that `Exercise` carries no type field
      (currently: "name, type and image, managed once...").
- [x] 8.3 Update `NavMenuTests.cs` and `HomeTests.cs` for the new nav link
      and the now-live `Home` tile.

## 9. Verification

- [x] 9.1 Run the Worker's vitest suite (`npm test` in `Trainfree.AdminApi`)
      -- all green.
- [x] 9.2 Run `dotnet build` for the solution -- clean, no new warnings; run
      `dotnet test Trainfree.slnx --configuration Release` -- all green.
- [x] 9.3 Manually run the app locally (`wrangler dev` on 9999 + Blazor dev
      server, after applying the new migrations to the local D1 with
      `npm run db:migrate:local`) and exercise the Exercises page end to end:
      empty state renders with the `Add Exercise` CTA; add creates and
      focuses a new row; renaming a second row to a name that collides
      case-insensitively surfaces the server's `409` as a row-level error
      without crashing; Revert restores the saved name and clears the
      error; Delete removes the row; deleting the last row returns to the
      empty state. Also confirm: sidebar shows `Home` / `Phases` /
      `Exercises` / `Programs` in that order with `Exercises` active on
      `/exercises`, and the `Home` page's `Exercises` tile navigates there.
      Stop both dev servers afterward.
