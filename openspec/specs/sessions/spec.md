# sessions Specification

## Purpose
TBD - created by archiving change add-sessions-crud. Update Purpose after archive.
## Requirements
### Requirement: Session identifier format
Each session SHALL be identified externally by a surrogate key in the form `SNN-`
followed by 6 Crockford base32 characters (e.g. `SNN-7K2QXM`), never by the table's
internal auto-incrementing row key. All API routes, request bodies, and response
bodies use this surrogate key as `id`.

#### Scenario: Generated ID shape
- **WHEN** the Worker creates a new session
- **THEN** the generated `id` matches `SNN-` followed by exactly 6 characters from the
  alphabet `ABCDEFGHJKMNPQRSTVWXYZ23456789`

### Requirement: List a program's sessions
The system SHALL provide `GET /api/programs/:programId/sessions`, returning all
sessions belonging to that program in creation order.

#### Scenario: Program has no sessions
- **WHEN** a client calls `GET /api/programs/:programId/sessions` for an existing
  program with no sessions
- **THEN** the Worker responds `200` with an empty JSON array

#### Scenario: Program has sessions
- **WHEN** a client calls `GET /api/programs/:programId/sessions` for a program with
  sessions
- **THEN** the Worker responds `200` with a JSON array of that program's sessions
  ordered by `created_at` ascending, excluding sessions belonging to other programs

#### Scenario: Program does not exist
- **WHEN** a client calls `GET /api/programs/:programId/sessions` for a `:programId`
  with no matching program
- **THEN** the Worker responds `404`

### Requirement: Session name length
A session's `name` SHALL be between 4 and 100 characters (inclusive) after trimming
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

### Requirement: Session name uniqueness within its program
A session's `name` SHALL be unique among sessions belonging to the same program,
compared case-insensitively. Sessions belonging to different programs may share a
name. This applies on both create and rename.

#### Scenario: Create with a name that already exists in the same program
- **WHEN** a client calls `POST /api/programs/:programId/sessions` with a `name`
  matching an existing session's name (case-insensitively) within the same program
- **THEN** the Worker responds `409` with a JSON error body and creates no row

#### Scenario: Create with a name that exists in a different program
- **WHEN** a client calls `POST /api/programs/:programId/sessions` with a `name`
  matching a session's name that belongs to a different program
- **THEN** the Worker creates the session and responds `201`

#### Scenario: Rename to a name that already exists on another session in the same program
- **WHEN** a client calls `PATCH /api/programs/:programId/sessions/:id` with a `name`
  matching another session's name (case-insensitively) within the same program
- **THEN** the Worker responds `409` with a JSON error body and makes no change

#### Scenario: Rename to the session's own current name
- **WHEN** a client calls `PATCH /api/programs/:programId/sessions/:id` with a `name`
  matching that same session's current name (e.g. only a change in trimmed whitespace)
- **THEN** the Worker responds `200` -- a session does not conflict with itself

### Requirement: Create a session
The system SHALL provide `POST /api/programs/:programId/sessions` to create a new
session under a program, with a system-generated ID.

#### Scenario: Valid name provided
- **WHEN** a client calls `POST /api/programs/:programId/sessions` for an existing
  program with a JSON body containing a `name` between 4 and 100 characters
- **THEN** the Worker creates a `sessions` row with a generated ID, that `program_id`,
  and current timestamps, and responds `201` with the created session

#### Scenario: Name fails length bound
- **WHEN** a client calls `POST /api/programs/:programId/sessions` with a missing
  `name`, or a `name` outside the 4-100 character bound
- **THEN** the Worker responds `400` with a JSON error body and creates no row

#### Scenario: Program does not exist
- **WHEN** a client calls `POST /api/programs/:programId/sessions` for a `:programId`
  with no matching program
- **THEN** the Worker responds `404` and creates no row

### Requirement: Rename a session
The system SHALL provide `PATCH /api/programs/:programId/sessions/:id` to update a
session's name.

#### Scenario: Valid rename
- **WHEN** a client calls `PATCH /api/programs/:programId/sessions/:id` for an existing
  session under that program with a `name` between 4 and 100 characters
- **THEN** the Worker updates the row's `name` and `updated_at`, and responds `200`
  with the updated session

#### Scenario: Session does not exist under that program
- **WHEN** a client calls `PATCH /api/programs/:programId/sessions/:id` for an `:id`
  with no matching session under `:programId` (including an `:id` that exists but
  belongs to a different program)
- **THEN** the Worker responds `404`

#### Scenario: Name fails length bound on rename
- **WHEN** a client calls `PATCH /api/programs/:programId/sessions/:id` with a `name`
  outside the 4-100 character bound
- **THEN** the Worker responds `400` with a JSON error body and makes no change

### Requirement: Delete a session
The system SHALL provide `DELETE /api/programs/:programId/sessions/:id` to remove a
session.

#### Scenario: Session exists
- **WHEN** a client calls `DELETE /api/programs/:programId/sessions/:id` for an
  existing session under that program
- **THEN** the Worker deletes the row and responds `204`

#### Scenario: Session does not exist under that program
- **WHEN** a client calls `DELETE /api/programs/:programId/sessions/:id` for an `:id`
  with no matching session under `:programId` (including an `:id` that exists but
  belongs to a different program)
- **THEN** the Worker responds `404`

### Requirement: Admin session rows nested under their program
The Blazor admin page SHALL display each program's sessions as rows nested beneath
that program's row, and allow creating, renaming, and deleting them without a full
page reload, using the same working/saved-value dirty-row pattern as program rows.

#### Scenario: Page loads with existing sessions
- **WHEN** the admin page loads and a program has sessions
- **THEN** it calls `GET /api/programs/:programId/sessions` for that program and
  renders one row per returned session, nested beneath that program's row

#### Scenario: Adding a session
- **WHEN** the admin user clicks a program's `Add Session` action
- **THEN** the page calls `POST /api/programs/:programId/sessions`, appends the
  returned session as a new row under that program, and places the name cell in an
  editable state

#### Scenario: Renaming a session
- **WHEN** the admin user edits a session row's name and clicks that row's `Save`
  button
- **THEN** the page calls `PATCH /api/programs/:programId/sessions/:id` with the new
  name and updates the row on success

#### Scenario: Save button appears on edit and hides after save
- **WHEN** the admin user edits a session row's name to a value different from the
  last-saved value
- **THEN** that row's `Save` button becomes visible, and is hidden again after a
  successful save

#### Scenario: Reverting discards an unsaved edit
- **WHEN** the admin user edits a session row's name and clicks that row's `Revert`
  button
- **THEN** the page restores the last-saved name in the row, hides `Save` and
  `Revert`, and makes no API call

#### Scenario: Deleting a session
- **WHEN** the admin user clicks a session row's `Delete` button
- **THEN** the page calls `DELETE /api/programs/:programId/sessions/:id` and removes
  the row from the list on success

#### Scenario: Save rejects a name that fails the length bound client-side
- **WHEN** the admin user clicks `Save` on a session row with a name outside the
  4-100 character bound
- **THEN** the page shows a validation error on that row and does not call
  `PATCH /api/programs/:programId/sessions/:id`

#### Scenario: Save surfaces a server-side rejection without crashing
- **WHEN** a `PATCH /api/programs/:programId/sessions/:id` call made by `Save` returns
  `400` or `409`
- **THEN** the page shows the returned error on that row and remains usable -- it does
  not throw an unhandled exception

