## 1. D1 migration

- [ ] 1.1 Add `migrations/0013_create_program_exercises.sql` creating `program_exercises`
      (surrogate `program_exercise_id`, FK `session_phase_id` -> `session_phases`
      cascade delete, FK `exercise_id` -> `exercises`, `type` discriminator, nullable
      `reps`/`duration_seconds`, `weight`, `sets`, `rest_seconds`, `side`,
      `created_at`/`updated_at`) and verify `wrangler d1 migrations apply --local`
      succeeds against the dev DB.

## 2. Worker: ids and validation

- [ ] 2.1 Add `PROGRAM_EXERCISE_PREFIX = "PGX-"` and
      `generateProgramExerciseId`/`isValidProgramExerciseId` to `ids.js`, following the
      existing `SESSION_PHASE_PREFIX` pattern; verify with a red-then-green test in
      `ids.test.js` asserting the generated shape and `isValidProgramExerciseId`
      round-trip.
- [ ] 2.2 Add program-exercise body validation to `validation.js` (or a new
      `program-exercises.js` helper): `exerciseId` required, `type` in
      `Reps`/`Timed`, the type-matched count (`reps` xor `durationSeconds`) required
      and > 0, `sets` > 0, `restSeconds` > 0, `weight` >= 0 when present, `side` in
      `Both`/`Left`/`Right` when present, and rejects a body carrying the
      type-mismatched count field; verify with a failing-first `validation.test.js`
      theory covering each rejection case before implementing.

## 3. Worker: program-exercises CRUD

- [ ] 3.1 Write a failing `session-phases.test.js`-style `program-exercises.test.js`
      for `listProgramExercises`/`programExerciseSessionPhaseExists` (scoped by
      `sessionPhaseId`, ordered by `created_at` then row id), then implement against
      real D1 via Miniflare until it's green.
- [ ] 3.2 Add a failing test for `createProgramExercise` (Reps and Timed cases,
      defaulting `weight` to `0` and `side` to `Both`), then implement.
- [ ] 3.3 Add a failing test for `updateProgramExercise` (rejects `exerciseId`/`type`
      in the body per Requirement: Update a program exercise's prescription; accepts
      partial updates to the remaining fields), then implement.
- [ ] 3.4 Add a failing test for `deleteProgramExercise` (unconditional, 404 when not
      found under that session phase), then implement.
- [ ] 3.5 Wire `GET/POST/PATCH/DELETE
      /api/programs/:programId/sessions/:sessionId/phases/:sessionPhaseId/exercises[/:id]`
      into `index.js`'s router, returning 404 through the existing
      program/session/session-phase chain checks; verify with `index.test.js` routing
      cases for each method plus the 404 chain.

## 4. Worker: exercises delete guard

- [ ] 4.1 Write a failing test in an `exercises.test.js` (new file, mirroring
      `phases.js`'s guard test) asserting `deleteExercise` throws when a
      `program_exercises` row references the exercise, then add the guard query to
      `exercises.js`'s `deleteExercise`, reusing or adding an `ExerciseInUseError` in
      `errors.js` alongside `PhaseInUseError`.
- [ ] 4.2 Verify `index.js`'s exercise delete route maps `ExerciseInUseError` to `409`
      (mirroring the existing `PhaseInUseError` -> `409` mapping) with an
      `index.test.js` case.

## 5. Domain: program exercise types

- [ ] 5.1 Add `ProgramExerciseId` to `Trainfree.Domain/Ids/` (`PGX-` prefix), following
      `SessionPhaseId`'s shape; verify with a red-then-green `ProgramExerciseIdTests.cs`
      in `Trainfree.Domain.Tests` covering parse success/failure and equality.
- [ ] 5.2 Add `RepsProgramExercise` and `TimedProgramExercise` as distinct
      `internal sealed` types (constructor-validated: reps/duration/sets/restSeconds >
      0, weight >= 0) per `CLAUDE-domain-driven-design.md`'s "no enum for state that
      carries different data" rule, with accompanying failing-first unit tests for each
      constructor guard.

## 6. Blazor: API client

- [ ] 6.1 Extend `ProgramsApiClient` (or its DTOs) with
      list/create/update/delete methods for program exercises under a session phase,
      deserializing into `RepsProgramExercise`/`TimedProgramExercise` based on the
      response's `type` discriminator; verify with failing-first
      `ProgramsApiClientTests.cs` cases for each method (success, 404, 400, 409 where
      applicable) using the existing fake `HttpClient` handler pattern.

## 7. Blazor: admin UI

- [ ] 7.1 Add a `ProgramExerciseRow` row model (mirroring `SessionPhaseRow`, but with
      working/saved values for reps-or-duration, weight, sets, restSeconds, side, and
      `IsDirty`) to `Programs.razor`, plus expand/collapse state per `SessionPhaseRow`
      defaulting expanded; verify with a failing-first `ProgramsPageTests.cs` bUnit
      case asserting rows render nested under their session phase.
- [ ] 7.2 Add the `Add Exercise` action revealing the inline add-exercise form
      (exercise dropdown, Reps/Timed choice, reps-or-duration/sets/restSeconds inputs,
      submit disabled until all required fields are positive), calling the create
      method on submit; verify with bUnit tests for the empty-library guidance case,
      the disabled-until-valid case, and the successful-submit case.
- [ ] 7.3 Render each program exercise row's cells per the spec's dash rules (a `0`
      numeric value renders as an en dash; a `TimedProgramExercise` row's `Reps` cell
      renders as a dash because the property doesn't exist, not because it's `0`);
      verify with bUnit tests for a Reps row with `weight = 0`, a Timed row, and a
      normal non-zero row.
- [ ] 7.4 Wire Save/Revert/Delete for program exercise rows through the dirty-row
      pattern, including surfacing a 400/409/404 error on the row without throwing;
      verify with bUnit tests for edit-then-save, revert, delete, and a
      server-error-on-save case.
- [ ] 7.5 Update the `Exercises` page's delete handler to surface the Worker's new
      `409` usage rejection on the row instead of removing it; verify with a
      failing-first `ExercisesPageTests.cs` case for the used-exercise delete attempt.

## 8. Full-suite verification

- [ ] 8.1 Run `dotnet test Trainfree.slnx --configuration Release` and the Worker's
      `vitest` suite; both green.
- [ ] 8.2 Manually walk through the `Programs` page in the running admin app: add a
      Reps and a Timed program exercise to a phase, edit and save each field, delete
      one, and confirm an in-use `Exercise` can no longer be deleted from the
      `Exercises` page.
