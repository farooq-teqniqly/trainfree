# exercises Specification

## Purpose
Exercises are the canonical exercise library (e.g. "Bodyweight Squat", "Skater
Jump") that program exercises pick from instead of typing free text. This spec
covers an exercise's externally visible identity and name rules, the Worker's
flat CRUD API over the `exercises` table, and the Blazor admin UI that manages
them. An exercise carries no `type` (Reps/Timed) and no image in this slice.
## Requirements
### Requirement: Exercise identifier format
Each exercise SHALL be identified externally by a surrogate key in the form
`EXR-` followed by 6 Crockford base32 characters (e.g. `EXR-7K2QXM`), never by
the table's internal auto-incrementing row key. All API routes, request
bodies, and response bodies use this surrogate key as `id`.

#### Scenario: Generated ID shape

- **WHEN** the Worker creates a new exercise
- **THEN** the generated `id` matches `EXR-` followed by exactly 6 characters
  from the alphabet `ABCDEFGHJKMNPQRSTVWXYZ23456789`

### Requirement: List exercises

The system SHALL provide `GET /api/exercises`, returning all exercises in
creation order. When two or more exercises share the same `created_at`
timestamp, the system SHALL break the tie by the `exercises` table's internal
auto-incrementing row key (not the external `id` surrogate returned in API
responses) ascending, so the order is deterministic across repeated calls and
matches insertion order.

#### Scenario: No exercises exist

- **WHEN** a client calls `GET /api/exercises` and no exercises exist
- **THEN** the Worker responds `200` with an empty JSON array

#### Scenario: Exercises exist

- **WHEN** a client calls `GET /api/exercises` and exercises exist
- **THEN** the Worker responds `200` with a JSON array of exercises ordered by
  `created_at` ascending

#### Scenario: Two exercises share the same created_at

- **WHEN** a client calls `GET /api/exercises` and two exercises have an
  identical `created_at` value
- **THEN** the Worker responds `200` with those two exercises ordered by the
  `exercises` table's internal row key (not the external `id` surrogate)
  ascending relative to each other, and this order is the same on every call

### Requirement: Exercise name length

An exercise's `name` SHALL be between 4 and 100 characters (inclusive) after
trimming leading/trailing whitespace. This bound applies on both create and
rename.

#### Scenario: Name too short

- **WHEN** a client submits a `name` that trims to fewer than 4 characters
  (including blank/whitespace-only)
- **THEN** the Worker responds `400` with a JSON error body and makes no
  change

#### Scenario: Name too long

- **WHEN** a client submits a `name` that trims to more than 100 characters
- **THEN** the Worker responds `400` with a JSON error body and makes no
  change

#### Scenario: Name at boundaries

- **WHEN** a client submits a `name` that trims to exactly 4 or exactly 100
  characters
- **THEN** the Worker accepts it

### Requirement: Exercise name uniqueness

An exercise's `name` SHALL be unique among all exercises, compared
case-insensitively. This applies on both create and rename.

#### Scenario: Create with a name that already exists

- **WHEN** a client calls `POST /api/exercises` with a `name` matching an
  existing exercise's name (case-insensitively)
- **THEN** the Worker responds `409` with a JSON error body and creates no row

#### Scenario: Rename to a name that already exists on another exercise

- **WHEN** a client calls `PATCH /api/exercises/:id` with a `name` matching
  another exercise's name (case-insensitively)
- **THEN** the Worker responds `409` with a JSON error body and makes no
  change

#### Scenario: Rename to the exercise's own current name

- **WHEN** a client calls `PATCH /api/exercises/:id` with a `name` matching
  that same exercise's current name (e.g. only a change in trimmed
  whitespace)
- **THEN** the Worker responds `200` -- an exercise does not conflict with
  itself

### Requirement: Create an exercise

The system SHALL provide `POST /api/exercises` to create a new exercise with
a system-generated ID.

#### Scenario: Valid name provided

- **WHEN** a client calls `POST /api/exercises` with a JSON body containing a
  `name` between 4 and 100 characters
- **THEN** the Worker creates an `exercises` row with a generated ID and
  current timestamps, and responds `201` with the created exercise

#### Scenario: Name fails length bound

- **WHEN** a client calls `POST /api/exercises` with a missing `name`, or a
  `name` outside the 4-100 character bound
- **THEN** the Worker responds `400` with a JSON error body and creates no
  row

### Requirement: Rename an exercise

The system SHALL provide `PATCH /api/exercises/:id` to update an exercise's
name.

#### Scenario: Valid rename

- **WHEN** a client calls `PATCH /api/exercises/:id` for an existing exercise
  with a `name` between 4 and 100 characters
- **THEN** the Worker updates the row's `name` and `updated_at`, and responds
  `200` with the updated exercise

#### Scenario: Exercise does not exist

- **WHEN** a client calls `PATCH /api/exercises/:id` for an `:id` with no
  matching exercise
- **THEN** the Worker responds `404`

#### Scenario: Name fails length bound on rename

- **WHEN** a client calls `PATCH /api/exercises/:id` with a `name` outside
  the 4-100 character bound
- **THEN** the Worker responds `400` with a JSON error body and makes no
  change

### Requirement: Delete an exercise

The system SHALL provide `DELETE /api/exercises/:id` to remove an exercise
unconditionally -- no other table references `exercises` yet, so no usage
guard applies in this capability.

#### Scenario: Exercise exists

- **WHEN** a client calls `DELETE /api/exercises/:id` for an existing
  exercise
- **THEN** the Worker deletes the row and responds `204`

#### Scenario: Exercise does not exist

- **WHEN** a client calls `DELETE /api/exercises/:id` for an `:id` with no
  matching exercise
- **THEN** the Worker responds `404`

### Requirement: Admin exercises page

The Blazor admin app SHALL provide an `Exercises` page at `/exercises`
listing every exercise as a row, using the same working/saved-value
dirty-row pattern as the Phases page, with no image column, no type column,
and no usage indicator or delete guard.

#### Scenario: Page loads with existing exercises

- **WHEN** the Exercises page loads and exercises exist
- **THEN** it calls `GET /api/exercises` and renders one row per returned
  exercise

#### Scenario: Page loads with no exercises

- **WHEN** the Exercises page loads and no exercises exist
- **THEN** it renders an empty-state view with an `Add Exercise` action and
  no table

#### Scenario: Adding an exercise

- **WHEN** the admin user clicks `Add Exercise`
- **THEN** the page calls `POST /api/exercises`, appends the returned
  exercise as a new row, and places the name cell in an editable state

#### Scenario: Renaming an exercise

- **WHEN** the admin user edits an exercise row's name and clicks that row's
  `Save` button
- **THEN** the page calls `PATCH /api/exercises/:id` with the new name and
  updates the row on success

#### Scenario: Save button appears on edit and hides after save

- **WHEN** the admin user edits an exercise row's name to a value different
  from the last-saved value
- **THEN** that row's `Save` button becomes visible, and is hidden again
  after a successful save

#### Scenario: Reverting discards an unsaved edit

- **WHEN** the admin user edits an exercise row's name and clicks that row's
  `Revert` button
- **THEN** the page restores the last-saved name in the row, hides `Save`
  and `Revert`, and makes no API call

#### Scenario: Deleting an exercise

- **WHEN** the admin user clicks an exercise row's `Delete` button
- **THEN** the page calls `DELETE /api/exercises/:id` and removes the row
  from the list on success -- no confirmation prompt or disabled state,
  since no usage guard exists in this capability

#### Scenario: Save rejects a name that fails the length bound client-side

- **WHEN** the admin user clicks `Save` on an exercise row with a name
  outside the 4-100 character bound
- **THEN** the page shows a validation error on that row and does not call
  `PATCH /api/exercises/:id`

#### Scenario: Save surfaces a server-side rejection without crashing

- **WHEN** a `PATCH /api/exercises/:id` call made by `Save` returns `400` or
  `409`
- **THEN** the page shows the returned error on that row and remains usable
  -- it does not throw an unhandled exception

#### Scenario: Load failure shows an error without crashing

- **WHEN** `GET /api/exercises` fails on page load
- **THEN** the page shows a load-failed message and remains usable
