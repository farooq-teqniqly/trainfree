## Purpose

Exercises are the canonical exercise library (e.g. "Bodyweight Squat", "Skater
Jump") that program exercises pick from instead of typing free text. This spec
covers an exercise's externally visible identity and name rules, the Worker's
flat CRUD API over the `exercises` table, and the Blazor admin UI that manages
them. An exercise carries no `type` (Reps/Timed) and no image in this slice --
see Decisions below.

## ADDED Requirements

### Requirement: Exercise identifier format
Each exercise SHALL be identified externally by a surrogate key in the form
`EXR-` followed by 6 Crockford base32 characters (e.g. `EXR-7K2QXM`), never by
the table's internal auto-incrementing row key. All API routes, request
bodies, and response bodies use this surrogate key as `id`.
**Rationale**: Matches the surrogate-key pattern already used by `programs`,
`sessions`, and `phases`, so no capability in this codebase exposes an
internal row key over the API.

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
**Rationale**: Matches the `phases` capability's list-ordering contract, so
admin pages present a stable, predictable row order without a separate sort
control.

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
**Rationale**: Matches the `phases` capability's name-length bound, keeping a
single validation rule across the two library entities rather than a
per-capability special case with no product reason to differ.

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
**Rationale**: Two library rows with the same name would be indistinguishable
in the picker slice 7 adds, defeating the point of a canonical library.

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
**Rationale**: Mirrors the `phases` capability's create contract.

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
**Rationale**: Mirrors the `phases` capability's rename contract.

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
**Rationale**: The `program_exercises` join that would make an exercise
"used" by a program doesn't exist until slice 7
(`add-program-categories-exercises-crud`); guarding delete against a
nonexistent reference would either be dead code or a placeholder that always
allows delete anyway. The guard is added in slice 7 alongside the join that
gives it a real condition to check, matching how the `phases` capability
itself waited for `rename-category-to-phase`/future joins rather than
guessing at the shape in advance.

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
**Rationale**: Matches `docs/design/admin-mockups/Exercises.dc.html`'s
structure minus the columns this slice explicitly defers (image to slice 13,
type because it was never an `Exercise`-level fact, usage to slice 7).

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

### Requirement: Admin shell navigation lands in final order

The Blazor admin app's sidebar SHALL include an `Exercises` link between
`Phases` and `Programs`, completing the sidebar's final nav order (`Home` /
`Phases` / `Exercises` / `Programs`), and the `Home` page SHALL show a third
quick-link tile for `Exercises` alongside the existing `Phases` and
`Programs` tiles.
**Rationale**: `docs/trainfree-roadmap.md` slice 4
(`restyle-admin-shell`) deliberately left the `Exercises` link and tile
absent until this slice existed to link to; this is the requirement that
retires that placeholder.

#### Scenario: Sidebar shows all three library links in order

- **WHEN** any admin page renders the sidebar
- **THEN** the nav items appear in the order `Home`, `Phases`, `Exercises`,
  `Programs`

#### Scenario: Home page's Exercises tile links to the Exercises page

- **WHEN** the admin user is on the `Home` page
- **THEN** an `Exercises` quick-link tile is shown alongside the `Phases` and
  `Programs` tiles, and clicking it navigates to `/exercises`

## Decisions

- **No `type` (Reps/Timed) field on `Exercise`.** The same exercise can be
  prescribed differently by different programs (e.g. sit-ups as 3 sets of 12
  reps in one program, max reps in 30 seconds in another), so `type` is a
  fact about a program's use of an exercise, not about the exercise itself.
  Putting it here would have meant either picking one type per exercise
  (wrong -- blocks the sit-ups scenario) or letting it be overridden per
  program (redundant -- the real value would live on `ProgramExercise`
  anyway, per slice 7). `docs/design/admin-mockups/Exercises.dc.html`'s Type
  column is dropped from the built page for this reason; the mockup itself
  was corrected to match (removed in this change's companion doc/mockup PR).
  Reconsider only if a future need arises for an exercise to have an
  intrinsic type independent of any program's use of it -- no such need is
  known today.
- **Delete is unconditional in this slice, not guarded.** The `phases`
  capability's precedent (unconditional delete until a future capability
  adds a real join to guard against) applies here for the identical reason:
  `program_exercises` -- the join that would make "used by a program" a real,
  queryable fact -- doesn't exist until slice 7. Building a guard now would
  mean querying a table that doesn't exist, or hand-rolling a placeholder
  that always returns "not used" until slice 7 replaces it. Slice 7 adds the
  guard together with the join, matching how it also adds the guard for
  `phases`.
- **No image upload affordance in this slice.** `docs/trainfree-roadmap.md`
  slice 13 (`add-exercise-images-r2`) wires up image storage; showing a
  disabled/inert upload button now (as the mockup's polished final-state
  view does) would be UI for a feature with no backend, and no way to
  exercise it in tests. Omitted entirely rather than rendered-and-disabled,
  matching how slice 5 (`phases`) quietly dropped the mockup's search box
  rather than shipping an inert one. Slice 13 adds the real control.
- **No search box or count-pill toolbar**, despite both appearing in
  `docs/design/admin-mockups/Exercises.dc.html`. The `phases` capability set
  this precedent already (slice 5's built `Phases.razor` has neither): a
  library page small enough to scan doesn't need a filter control, and one
  can be added later against real usage data if the list grows unwieldy. No
  requirement above depends on it existing.

## Requirement coverage

Anchor: `docs/trainfree-roadmap.md` slice 6 (`add-exercise-library-crud`)

| # | Anchor requirement | Covered by |
|---|--------------------|-----------|
| 1 | Admin CRUD for a canonical `Exercise` entity (name only) | Req: Create an exercise, Rename an exercise, Delete an exercise, List exercises |
| 2 | D1 migration: `exercises` table | Not covered by spec -- schema/migration is an implementation detail; see tasks.md |
| 3 | Worker: `GET/POST/PATCH/DELETE /api/exercises` | Req: List exercises, Create an exercise, Rename an exercise, Delete an exercise |
| 4 | Delete is unconditional in this slice (guard deferred to slice 7) | Req: Delete an exercise; see Decisions |
| 5 | Blazor: new `Exercises` page | Req: Admin exercises page |
| 6 | Sidebar nav lands in final order (`Home`/`Phases`/`Exercises`/`Programs`) | Req: Admin shell navigation lands in final order |
| 7 | `Home` page's third quick-link tile completed | Req: Admin shell navigation lands in final order |
| 8 | Image upload affordance omitted (deferred to slice 13) | Not covered by spec -- absence of a feature isn't a testable requirement; see Decisions |
| 9 | No `type` field (moved to slice 7's `ProgramExercise`) | Not covered by spec -- absence of a field isn't a testable requirement; see Decisions |
