# phases Specification

## Purpose
Phases are the canonical phase library (e.g. "Warm Up", "Legs") that program
sessions pick from instead of typing free text. This spec covers a phase's externally
visible identity and name rules, the Worker's flat CRUD API over the `phases` table,
and the Blazor admin UI that manages them. Delete is unconditional here -- no other table
references `phases` yet; a usage guard is expected once a future capability adds a
join from sessions to phases.
## Requirements
### Requirement: Phase identifier format
Each phase SHALL be identified externally by a surrogate key in the form `PHS-` followed
by 6 Crockford base32 characters (e.g. `PHS-7K2QXM`), never by the table's internal
auto-incrementing row key. All API routes, request bodies, and response bodies use this
surrogate key as `id`.

#### Scenario: Generated ID shape

- **WHEN** the Worker creates a new phase
- **THEN** the generated `id` matches `PHS-` followed by exactly 6 characters from the
  alphabet `ABCDEFGHJKMNPQRSTVWXYZ23456789`

### Requirement: List phases

The system SHALL provide `GET /api/phases`, returning all phases in creation order.
When two or more phases share the same `created_at` timestamp, the system SHALL break
the tie by the `phases` table's internal auto-incrementing row key (not the external
`id` surrogate returned in API responses) ascending, so the order is deterministic
across repeated calls and matches insertion order.

#### Scenario: No phases exist

- **WHEN** a client calls `GET /api/phases` and no phases exist
- **THEN** the Worker responds `200` with an empty JSON array

#### Scenario: Phases exist

- **WHEN** a client calls `GET /api/phases` and phases exist
- **THEN** the Worker responds `200` with a JSON array of phases ordered by `created_at`
  ascending

#### Scenario: Two phases share the same created_at

- **WHEN** a client calls `GET /api/phases` and two phases have an identical
  `created_at` value
- **THEN** the Worker responds `200` with those two phases ordered by the `phases`
  table's internal row key (not the external `id` surrogate) ascending relative to
  each other, and this order is the same on every call

### Requirement: Phase name length

A phase's `name` SHALL be between 4 and 100 characters (inclusive) after trimming
leading/trailing whitespace. This bound applies on both create and rename.

#### Scenario: Name too short

- **WHEN** a client submits a `name` that trims to fewer than 4 characters (including
  blank/whitespace-only)
- **THEN** the Worker responds `400` with a JSON error body and makes no change

#### Scenario: Name too long

- **WHEN** a client submits a `name` that trims to more than 100 characters
- **THEN** the Worker responds `400` with a JSON error body and makes no change

#### Scenario: Name at boundaries

- **WHEN** a client submits a `name` that trims to exactly 4 or exactly 100 characters
- **THEN** the Worker accepts it

### Requirement: Phase name uniqueness

A phase's `name` SHALL be unique among all phases, compared case-insensitively. This
applies on both create and rename.

#### Scenario: Create with a name that already exists

- **WHEN** a client calls `POST /api/phases` with a `name` matching an existing phase's
  name (case-insensitively)
- **THEN** the Worker responds `409` with a JSON error body and creates no row

#### Scenario: Rename to a name that already exists on another phase

- **WHEN** a client calls `PATCH /api/phases/:id` with a `name` matching another phase's
  name (case-insensitively)
- **THEN** the Worker responds `409` with a JSON error body and makes no change

#### Scenario: Rename to the phase's own current name

- **WHEN** a client calls `PATCH /api/phases/:id` with a `name` matching that same
  phase's current name (e.g. only a change in trimmed whitespace)
- **THEN** the Worker responds `200` -- a phase does not conflict with itself

### Requirement: Create a phase

The system SHALL provide `POST /api/phases` to create a new phase with a
system-generated ID.

#### Scenario: Valid name provided

- **WHEN** a client calls `POST /api/phases` with a JSON body containing a `name` between
  4 and 100 characters
- **THEN** the Worker creates a `phases` row with a generated ID and current timestamps,
  and responds `201` with the created phase

#### Scenario: Name fails length bound

- **WHEN** a client calls `POST /api/phases` with a missing `name`, or a `name` outside
  the 4-100 character bound
- **THEN** the Worker responds `400` with a JSON error body and creates no row

### Requirement: Rename a phase

The system SHALL provide `PATCH /api/phases/:id` to update a phase's name.

#### Scenario: Valid rename

- **WHEN** a client calls `PATCH /api/phases/:id` for an existing phase with a `name`
  between 4 and 100 characters
- **THEN** the Worker updates the row's `name` and `updated_at`, and responds `200` with
  the updated phase

#### Scenario: Phase does not exist

- **WHEN** a client calls `PATCH /api/phases/:id` for an `:id` with no matching phase
- **THEN** the Worker responds `404`

#### Scenario: Name fails length bound on rename

- **WHEN** a client calls `PATCH /api/phases/:id` with a `name` outside the 4-100
  character bound
- **THEN** the Worker responds `400` with a JSON error body and makes no change

### Requirement: Delete a phase

The system SHALL provide `DELETE /api/phases/:id` to remove a phase unconditionally --
no other table references `phases` yet, so no usage guard applies in this capability.

#### Scenario: Phase exists

- **WHEN** a client calls `DELETE /api/phases/:id` for an existing phase
- **THEN** the Worker deletes the row and responds `204`

#### Scenario: Phase does not exist

- **WHEN** a client calls `DELETE /api/phases/:id` for an `:id` with no matching phase
- **THEN** the Worker responds `404`

### Requirement: Admin phases page

The Blazor admin app SHALL provide a `Phases` page at `/phases` listing every phase as a
row, using the same working/saved-value dirty-row pattern as the Programs page, with no
usage indicator or delete guard.

#### Scenario: Page loads with existing phases

- **WHEN** the Phases page loads and phases exist
- **THEN** it calls `GET /api/phases` and renders one row per returned phase

#### Scenario: Page loads with no phases

- **WHEN** the Phases page loads and no phases exist
- **THEN** it renders an empty-state view with an `Add Phase` action and no table

#### Scenario: Adding a phase

- **WHEN** the admin user clicks `Add Phase`
- **THEN** the page calls `POST /api/phases`, appends the returned phase as a new row,
  and places the name cell in an editable state

#### Scenario: Renaming a phase

- **WHEN** the admin user edits a phase row's name and clicks that row's `Save` button
- **THEN** the page calls `PATCH /api/phases/:id` with the new name and updates the row
  on success

#### Scenario: Save button appears on edit and hides after save

- **WHEN** the admin user edits a phase row's name to a value different from the
  last-saved value
- **THEN** that row's `Save` button becomes visible, and is hidden again after a
  successful save

#### Scenario: Reverting discards an unsaved edit

- **WHEN** the admin user edits a phase row's name and clicks that row's `Revert` button
- **THEN** the page restores the last-saved name in the row, hides `Save` and `Revert`,
  and makes no API call

#### Scenario: Deleting a phase

- **WHEN** the admin user clicks a phase row's `Delete` button
- **THEN** the page calls `DELETE /api/phases/:id` and removes the row from the list on
  success -- no confirmation prompt or disabled state, since no usage guard exists in
  this capability

#### Scenario: Save rejects a name that fails the length bound client-side

- **WHEN** the admin user clicks `Save` on a phase row with a name outside the 4-100
  character bound
- **THEN** the page shows a validation error on that row and does not call
  `PATCH /api/phases/:id`

#### Scenario: Save surfaces a server-side rejection without crashing

- **WHEN** a `PATCH /api/phases/:id` call made by `Save` returns `400` or `409`
- **THEN** the page shows the returned error on that row and remains usable -- it does
  not throw an unhandled exception

#### Scenario: Load failure shows an error without crashing

- **WHEN** `GET /api/phases` fails on page load
- **THEN** the page shows a load-failed message and remains usable

