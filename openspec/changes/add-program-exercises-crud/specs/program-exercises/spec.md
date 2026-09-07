## Purpose

A program exercise is one `SessionPhase`'s instance of a canonical `Exercise` from the
exercise library, carrying the reps-or-timed prescription (reps/duration, weight, sets,
rest, side) that makes it specific to that session's use of the phase, distinct from two
other sessions that both happen to use the same phase and exercise.

## ADDED Requirements

### Requirement: Program exercise identifier format
Each program exercise SHALL be identified externally by a surrogate key in the form
`PGX-` followed by 6 Crockford base32 characters (e.g. `PGX-7K2QXM`), never by the
table's internal auto-incrementing row key. All API routes, request bodies, and response
bodies use this surrogate key as `id`.
**Rationale**: Matches the surrogate-key convention already used by `programs`,
`sessions`, `phases`, `exercises`, and `session_phases`.

#### Scenario: Generated ID shape
- **WHEN** the Worker creates a new program exercise
- **THEN** the generated `id` matches `PGX-` followed by exactly 6 characters from the
  alphabet `ABCDEFGHJKMNPQRSTVWXYZ23456789`

### Requirement: Program exercise is one of two distinct types
A program exercise SHALL be either a `RepsProgramExercise` (fields: `reps` integer > 0,
`weight` fractional number >= 0 in lbs, `sets` integer > 0, `restSeconds` integer > 0,
`side` one of `Both`/`Left`/`Right`) or a `TimedProgramExercise` (`durationSeconds`
integer > 0 in place of `reps`, plus the same `weight`/`sets`/`restSeconds`/`side`
fields). A `RepsProgramExercise` response body SHALL NOT include a `durationSeconds`
property, and a `TimedProgramExercise` response body SHALL NOT include a `reps`
property.
**Rationale**: `type` is a fact about how a program prescribes an exercise (the same
canonical exercise can be Reps in one program and Timed in another), not about the
exercise itself, and the two shapes carry different required fields -- a single type
with a `type` enum and nullable `reps`/`durationSeconds` columns side by side would
violate the "no enum for state that carries different data" rule. Neither type carries a
`note` field; that is deferred past this change.

#### Scenario: Reps type omits durationSeconds
- **WHEN** the Worker returns a `RepsProgramExercise`
- **THEN** the JSON body includes `reps` and has no `durationSeconds` property

#### Scenario: Timed type omits reps
- **WHEN** the Worker returns a `TimedProgramExercise`
- **THEN** the JSON body includes `durationSeconds` and has no `reps` property

### Requirement: List a session phase's program exercises
The system SHALL provide
`GET /api/programs/:programId/sessions/:sessionId/phases/:sessionPhaseId/exercises`,
returning all program exercises belonging to that session phase in creation order, each
including the `exerciseId` it references.
**Rationale**: A program exercise has no name of its own; the admin UI resolves each
row's display name by matching `exerciseId` against the already-loaded exercise
library, so the Worker does not need to join and embed an `exerciseName`.

#### Scenario: Session phase has no program exercises
- **WHEN** a client calls the list route for an existing session phase with no program
  exercises
- **THEN** the Worker responds `200` with an empty JSON array

#### Scenario: Session phase has program exercises
- **WHEN** a client calls the list route for a session phase with program exercises
- **THEN** the Worker responds `200` with a JSON array ordered by `created_at`
  ascending, excluding program exercises belonging to other session phases

#### Scenario: Session phase does not exist under that session/program
- **WHEN** a client calls the list route for a `:sessionPhaseId` with no matching
  session phase under that `:sessionId`/`:programId` chain
- **THEN** the Worker responds `404`

### Requirement: Create a program exercise
The system SHALL provide
`POST /api/programs/:programId/sessions/:sessionId/phases/:sessionPhaseId/exercises` to
add an exercise to a session phase, with a system-generated ID. The request body SHALL
include `exerciseId` (referencing an existing exercise), `type` (`Reps` or `Timed`),
`sets`, `restSeconds`, and either `reps` (when `type` is `Reps`) or `durationSeconds`
(when `type` is `Timed`). The Worker SHALL default `weight` to `0` and `side` to `Both`
when omitted.
**Rationale**: `reps`/`durationSeconds`, `sets`, and `restSeconds` are all required to be
strictly positive, so unlike a bare `SessionPhase` reference, a program exercise cannot
be created with valid zero-value defaults for those fields -- the admin UI collects them
in a small add-exercise form up front instead of creating a bare row and editing it in
afterward. `weight` (>= 0, so `0` is valid) and `side` (defaults meaningfully to `Both`)
have no such constraint and so are left out of that form.

#### Scenario: Valid Reps program exercise
- **WHEN** a client calls the create route with a JSON body of
  `{ exerciseId, type: "Reps", reps, sets, restSeconds }` where `exerciseId` matches an
  existing exercise and `reps`/`sets`/`restSeconds` are all positive integers
- **THEN** the Worker creates a `program_exercises` row with a generated ID, `weight`
  `0`, `side` `Both`, and responds `201` with the created `RepsProgramExercise`

#### Scenario: Valid Timed program exercise
- **WHEN** a client calls the create route with a JSON body of
  `{ exerciseId, type: "Timed", durationSeconds, sets, restSeconds }` where `exerciseId`
  matches an existing exercise and `durationSeconds`/`sets`/`restSeconds` are all
  positive integers
- **THEN** the Worker creates a `program_exercises` row with a generated ID, `weight`
  `0`, `side` `Both`, and responds `201` with the created `TimedProgramExercise`

#### Scenario: Session phase does not exist under that session/program
- **WHEN** a client calls the create route for a `:sessionPhaseId` with no matching
  session phase under that `:sessionId`/`:programId` chain
- **THEN** the Worker responds `404` and creates no row

#### Scenario: exerciseId is missing or does not reference an existing exercise
- **WHEN** a client calls the create route with a missing `exerciseId`, or an
  `exerciseId` that matches no exercise
- **THEN** the Worker responds `400` with a JSON error body and creates no row

#### Scenario: type is missing or not Reps/Timed
- **WHEN** a client calls the create route with a missing `type`, or a `type` other than
  `Reps` or `Timed`
- **THEN** the Worker responds `400` with a JSON error body and creates no row

#### Scenario: Required numeric field missing or not strictly positive
- **WHEN** a client calls the create route with a missing, zero, or negative `reps`
  (for `type: "Reps"`), `durationSeconds` (for `type: "Timed"`), `sets`, or
  `restSeconds`
- **THEN** the Worker responds `400` with a JSON error body and creates no row

#### Scenario: Reps type body includes durationSeconds, or Timed type body includes reps
- **WHEN** a client calls the create route with `type: "Reps"` and a `durationSeconds`
  property, or `type: "Timed"` and a `reps` property
- **THEN** the Worker responds `400` with a JSON error body and creates no row

#### Scenario: weight is negative
- **WHEN** a client calls the create route with a negative `weight`
- **THEN** the Worker responds `400` with a JSON error body and creates no row

#### Scenario: side is not Both/Left/Right
- **WHEN** a client calls the create route with a `side` other than `Both`, `Left`, or
  `Right`
- **THEN** the Worker responds `400` with a JSON error body and creates no row

#### Scenario: The same exercise is added to a session phase twice
- **WHEN** a client calls the create route with an `exerciseId` that already has a
  program exercise row under that same session phase
- **THEN** the Worker creates a second, independent program exercise row and responds
  `201`

### Requirement: Update a program exercise's prescription
The system SHALL provide
`PATCH /api/programs/:programId/sessions/:sessionId/phases/:sessionPhaseId/exercises/:id`
to update `reps`/`durationSeconds` (whichever the row's type carries), `weight`,
`sets`, `restSeconds`, and/or `side`. The Worker SHALL reject a request body containing
`exerciseId` or `type` with `400` and make no change.
**Rationale**: Unlike a `SessionPhase` (which carries no editable data beyond its
reference), a program exercise's prescription is edited inline after creation via the
same dirty-row pattern as every other admin field. `type` is fixed at creation --
switching Reps/Timed is a distinct-enough change (different required fields, different
domain type) that it is modeled as delete-and-recreate rather than an in-place
conversion.

#### Scenario: Valid update
- **WHEN** a client calls the update route for an existing program exercise with a body
  updating one or more of `reps`/`durationSeconds`, `weight`, `sets`, `restSeconds`,
  `side` to valid values
- **THEN** the Worker updates the row and responds `200` with the updated program
  exercise

#### Scenario: Program exercise does not exist under that session phase
- **WHEN** a client calls the update route for an `:id` with no matching program
  exercise under that `:sessionPhaseId`
- **THEN** the Worker responds `404`

#### Scenario: Update attempts to change exerciseId or type
- **WHEN** a client calls the update route with an `exerciseId` or `type` property in
  the body
- **THEN** the Worker responds `400` with a JSON error body and makes no change

#### Scenario: Update sets a required numeric field to zero, negative, or missing-equivalent
- **WHEN** a client calls the update route setting `reps`/`durationSeconds` (whichever
  applies to the row's type), `sets`, or `restSeconds` to a non-positive value
- **THEN** the Worker responds `400` with a JSON error body and makes no change

#### Scenario: Update sets weight negative or side to an invalid value
- **WHEN** a client calls the update route setting `weight` to a negative number, or
  `side` to a value other than `Both`/`Left`/`Right`
- **THEN** the Worker responds `400` with a JSON error body and makes no change

### Requirement: Delete a program exercise
The system SHALL provide
`DELETE /api/programs/:programId/sessions/:sessionId/phases/:sessionPhaseId/exercises/:id`
to remove a program exercise unconditionally -- nothing references `program_exercises`
yet.

#### Scenario: Program exercise exists
- **WHEN** a client calls the delete route for an existing program exercise under that
  session phase
- **THEN** the Worker deletes the row and responds `204`

#### Scenario: Program exercise does not exist under that session phase
- **WHEN** a client calls the delete route for an `:id` with no matching program
  exercise under that `:sessionPhaseId`
- **THEN** the Worker responds `404`

### Requirement: Deleting a session phase cascades to its program exercises
When a session phase is deleted, the system SHALL also delete every program exercise
belonging to it.
**Rationale**: A program exercise has no independent existence or meaning outside its
parent session phase, matching the existing cascade from a deleted session to its
session phases.

#### Scenario: Deleting a session phase with program exercises
- **WHEN** the admin user deletes a session phase that has one or more program
  exercises
- **THEN** `DELETE /api/programs/:programId/sessions/:sessionId/phases/:id` succeeds and
  every program exercise that belonged to it is also removed

### Requirement: Admin program exercise rows nested under their session phase
The Blazor admin page SHALL display each session phase's program exercises as rows
nested beneath that session phase's row, using the same chevron expand/collapse pattern
already used for a session's phases. Each row SHALL show: the exercise's name (picked
from a plain dropdown of the exercise library, no search box, no inline "New
exercise..." shortcut), a `Reps`/`Timed` type badge, the reps-or-duration cell, weight,
sets, rest seconds, and side -- all editable inline via the same working/saved-value
dirty-row pattern as other admin rows, except the exercise reference and type, which are
fixed after creation. A numeric cell SHALL render as an en dash (`–`) when its
value is `0`; a `TimedProgramExercise` row's `Reps` cell SHALL also render as a dash, for
the different reason that the type has no `Reps` property at all -- the page SHALL NOT
carry a spurious `Reps = 0` on that type to produce the dash.
**Rationale**: Reuses the nesting and dirty-row patterns already established one level
up; the two different reasons for showing a dash (an actual zero value vs. a
type-absent property) matter because the client's `RepsProgramExercise`/
`TimedProgramExercise` domain types must stay faithful to the DDD "no nullable field for
data only some states carry" rule even though the rendered result looks identical.

#### Scenario: Page loads with existing program exercises
- **WHEN** the admin page loads and a session phase has program exercises
- **THEN** it calls the list route for that session phase and renders one row per
  returned program exercise, nested beneath that session phase's row

#### Scenario: One session phase's program exercises fail to load
- **WHEN** the admin page loads and the list route fails for one session phase
- **THEN** that session phase's row still renders (with no exercise rows) alongside a
  per-row load-failed message, and every other row still renders normally

#### Scenario: Adding a program exercise opens a small form
- **WHEN** the admin user clicks a session phase's `Add Exercise` action
- **THEN** the page shows an inline form collecting an exercise (dropdown from the
  exercise library), a Reps/Timed type choice, the reps-or-duration count, sets, and
  rest seconds -- weight and side are not part of the form

#### Scenario: The exercise library is empty
- **WHEN** the admin user clicks a session phase's `Add Exercise` action and no
  exercises exist in the library
- **THEN** the page shows guidance to create an exercise on the `Exercises` page first,
  and shows no add-exercise form

#### Scenario: Submitting the add-exercise form
- **WHEN** the admin user fills every field in the add-exercise form with valid values
  and submits it
- **THEN** the page calls the create route with those values and appends the returned
  program exercise as a new row, defaulting weight to a dash (`0`) and side to `Both`

#### Scenario: Add-exercise form blocks submission until required fields are valid
- **WHEN** the admin user has not filled every required field in the add-exercise form
  with a positive value, or has not chosen an exercise
- **THEN** the page does not call the create route

#### Scenario: Editing a program exercise's prescription
- **WHEN** the admin user edits a program exercise row's reps-or-duration, weight,
  sets, rest, or side, and clicks that row's `Save` button
- **THEN** the page calls the update route with the changed field(s) and updates the
  row on success

#### Scenario: Save rejects a non-positive required field client-side
- **WHEN** the admin user clicks `Save` on a program exercise row with a
  reps-or-duration, sets, or rest value that is zero or negative
- **THEN** the page shows a validation error on that row and does not call the update
  route

#### Scenario: Reverting discards an unsaved edit
- **WHEN** the admin user edits a program exercise row and clicks that row's `Revert`
  button
- **THEN** the page restores the last-saved values in the row, hides `Save` and
  `Revert`, and makes no API call

#### Scenario: Deleting a program exercise
- **WHEN** the admin user clicks a program exercise row's `Delete` button
- **THEN** the page calls the delete route and removes the row from the list on success

#### Scenario: Delete or save surfaces a server-side failure without crashing
- **WHEN** a create, update, or delete call for a program exercise returns a non-2xx
  response
- **THEN** the page shows the returned error on that row or form and remains usable --
  it does not throw an unhandled exception

## Decisions

- **Small add-time form instead of create-bare-then-edit.** Every other admin entity in
  this app (`Program`, `Session`, `Phase`, `Exercise`, `SessionPhase`) is created bare
  and edited inline afterward, because none of them has a field with a positive-value
  constraint that a sensible zero-value default could violate. `ProgramExercise`'s
  `reps`/`durationSeconds`/`sets`/`restSeconds` are all required to be strictly
  positive, so the bare-then-edit pattern cannot produce a valid row. A small form
  collecting exactly the fields that cannot default validly (exercise, type, the count,
  sets, rest) preserves the pattern for `weight`/`side`, which can default validly and
  so stay purely inline-editable. The alternative -- seeding a new row with fabricated
  positive defaults (e.g. `reps = 1`) and creating it immediately like a `SessionPhase`
  -- was rejected because a fabricated `reps = 1` looks like real, meaningful data
  rather than an unset placeholder, and a user skimming the sheet has no way to tell the
  two apart.

- **`type` is immutable after creation (delete-and-recreate to change it).** Converting
  a `RepsProgramExercise` row to a `TimedProgramExercise` in place would mean an update
  request that simultaneously removes one required field and adds another required
  field with no valid default -- a fundamentally different operation from every other
  `PATCH` in this app, which only ever adjusts existing fields' values. Treating a type
  change as delete-and-recreate keeps `PATCH`'s contract uniform (same set of fields
  present before and after) across every admin capability. This would be worth
  revisiting if usage showed people frequently reclassifying an exercise's type instead
  of getting it right at creation.

- **One `program_exercises` D1 table with a discriminator column, not two tables.** The
  DDD "no enum for state that carries different data" rule governs the client's C#
  domain types (`RepsProgramExercise`/`TimedProgramExercise` as distinct classes), not
  the physical SQL schema -- a single table with a `type` discriminator plus nullable
  `reps`/`duration_seconds` columns is the same storage-layer compromise `phases`/
  `session_phases` already accept elsewhere (surrogate keys over row keys, JS Worker
  code with no equivalent DDD enforcement). Two tables would need a `UNION`-shaped list
  query and would not, on its own, buy back any type safety the JS Worker layer does not
  already forgo.

## Requirement coverage

Anchor: `docs/trainfree-roadmap.md`, slice 8 (`add-program-exercises-crud`)

| # | Anchor requirement | Covered by |
|---|--------------------|-----------|
| 1 | Per-`SessionPhase` `ProgramExercise` join referencing an `Exercise`, completing the full spreadsheet | Req: Admin program exercise rows nested under their session phase |
| 2 | Keyed to a specific `SessionPhase` row, not the canonical `Phase` -- independent lists per session | Req: List a session phase's program exercises; Req: Create a program exercise (route is nested under `:sessionPhaseId`) |
| 3 | `type` lives on `ProgramExercise`, not `Exercise`; `RepsProgramExercise`/`TimedProgramExercise` as distinct types, not an enum with nullable columns | Req: Program exercise is one of two distinct types; Decisions |
| 4 | `RepsProgramExercise` (reps>0, weight>=0, sets>0, restSeconds>0, side) and `TimedProgramExercise` (durationSeconds>0 in place of reps, same remaining fields) | Req: Program exercise is one of two distinct types; Req: Create a program exercise |
| 5 | Neither type carries a `note` field (deferred) | Req: Program exercise is one of two distinct types (Rationale) |
| 6 | `side` defaults to `Both` on create (other values `Left`/`Right`) | Req: Create a program exercise |
| 7 | A numeric cell (e.g. `Weight`) renders as an en dash when its value is `0` | Req: Admin program exercise rows nested under their session phase |
| 8 | A `TimedProgramExercise` row's `Reps` cell renders as a dash because the type has no `Reps` property, and the UI must not fake this with `Reps = 0` | Req: Admin program exercise rows nested under their session phase |
| 9 | Adds the `Exercise` delete guard deferred from slice 6, same global-entity reasoning as slice 7's `Phase` guard | See `exercises` capability's MODIFIED delta in this change |
| 10 | Exercise row's name picked from a plain dropdown (no search, no inline "New exercise..." shortcut); per-row `Image` column from the original mockup is gone | Req: Admin program exercise rows nested under their session phase |
| 11 | D1 migration: `program_exercises` table (FK `session_phase_id`, `exercise_id`, discriminator + type-specific columns; cascade-deleted when parent `SessionPhase` is deleted) | Req: Deleting a session phase cascades to its program exercises; Decisions |
| 12 | Worker: nested routes `.../phases/:sessionPhaseId/exercises` | Req: List/Create/Update/Delete a program exercise |
| 13 | Blazor: phase rows gain expand/collapse revealing exercise rows -- full inline-editable spreadsheet, same dirty-row pattern | Req: Admin program exercise rows nested under their session phase |
| 14 | This is the last purely-admin slice | Not covered -- a statement about the roadmap's sequencing, not a testable behavior of this change |
