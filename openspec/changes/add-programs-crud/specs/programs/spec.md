## ADDED Requirements

### Requirement: Program identifier format
Each program SHALL be identified externally by a surrogate key in the form `PRG-`
followed by 6 Crockford base32 characters (e.g. `PRG-7K2QXM`), never by the table's
internal auto-incrementing row key. All API routes, request bodies, and response
bodies use this surrogate key as `id`.

#### Scenario: Generated ID shape
- **WHEN** the Worker creates a new program
- **THEN** the generated `id` matches `PRG-` followed by exactly 6 characters from the
  alphabet `ABCDEFGHJKMNPQRSTVWXYZ23456789`

### Requirement: List programs
The system SHALL provide `GET /api/programs`, returning all programs in creation order.

#### Scenario: No programs exist
- **WHEN** a client calls `GET /api/programs` and the `programs` table is empty
- **THEN** the Worker responds `200` with an empty JSON array

#### Scenario: Programs exist
- **WHEN** a client calls `GET /api/programs` and programs exist
- **THEN** the Worker responds `200` with a JSON array of programs ordered by
  `created_at` ascending

### Requirement: Program name length
A program's `name` SHALL be between 5 and 100 characters (inclusive) after trimming
leading/trailing whitespace. This bound applies on both create and rename.

#### Scenario: Name too short
- **WHEN** a client submits a `name` that trims to fewer than 5 characters (including
  blank/whitespace-only)
- **THEN** the Worker responds `400` with a JSON error body and makes no change

#### Scenario: Name too long
- **WHEN** a client submits a `name` that trims to more than 100 characters
- **THEN** the Worker responds `400` with a JSON error body and makes no change

#### Scenario: Name at boundaries
- **WHEN** a client submits a `name` that trims to exactly 5 or exactly 100 characters
- **THEN** the Worker accepts it

### Requirement: Program name uniqueness
A program's `name` SHALL be unique among all programs, compared case-insensitively.
This applies on both create and rename.

#### Scenario: Create with a name that already exists
- **WHEN** a client calls `POST /api/programs` with a `name` matching an existing
  program's name case-insensitively
- **THEN** the Worker responds `409` with a JSON error body and creates no row

#### Scenario: Rename to a name that already exists on another program
- **WHEN** a client calls `PATCH /api/programs/:id` with a `name` matching another
  program's name case-insensitively
- **THEN** the Worker responds `409` with a JSON error body and makes no change

#### Scenario: Rename to the program's own current name
- **WHEN** a client calls `PATCH /api/programs/:id` with a `name` matching that same
  program's current name (e.g. only a change in trimmed whitespace)
- **THEN** the Worker responds `200` -- a program does not conflict with itself

### Requirement: Create a program
The system SHALL provide `POST /api/programs` to create a new program with a
system-generated ID.

#### Scenario: Valid name provided
- **WHEN** a client calls `POST /api/programs` with a JSON body containing a `name`
  between 5 and 100 characters
- **THEN** the Worker creates a `programs` row with a generated ID and current
  timestamps, and responds `201` with the created program

#### Scenario: Name fails length bound
- **WHEN** a client calls `POST /api/programs` with a missing `name`, or a `name`
  outside the 5-100 character bound
- **THEN** the Worker responds `400` with a JSON error body and creates no row

### Requirement: Rename a program
The system SHALL provide `PATCH /api/programs/:id` to update a program's name.

#### Scenario: Valid rename
- **WHEN** a client calls `PATCH /api/programs/:id` for an existing program with a
  `name` between 5 and 100 characters
- **THEN** the Worker updates the row's `name` and `updated_at`, and responds `200`
  with the updated program

#### Scenario: Program does not exist
- **WHEN** a client calls `PATCH /api/programs/:id` for an ID with no matching row
- **THEN** the Worker responds `404`

#### Scenario: Name fails length bound on rename
- **WHEN** a client calls `PATCH /api/programs/:id` with a `name` outside the 5-100
  character bound
- **THEN** the Worker responds `400` with a JSON error body and makes no change

### Requirement: Delete a program
The system SHALL provide `DELETE /api/programs/:id` to remove a program.

#### Scenario: Program exists
- **WHEN** a client calls `DELETE /api/programs/:id` for an existing program
- **THEN** the Worker deletes the row and responds `204`

#### Scenario: Program does not exist
- **WHEN** a client calls `DELETE /api/programs/:id` for an ID with no matching row
- **THEN** the Worker responds `404`

### Requirement: Admin program list UI
The Blazor admin page SHALL display all programs as rows and allow creating, renaming,
and deleting them without a full page reload.

#### Scenario: Page loads with existing programs
- **WHEN** the admin page loads
- **THEN** it calls `GET /api/programs` and renders one row per returned program,
  showing its name

#### Scenario: Adding a program
- **WHEN** the admin user clicks `[+ Program]`
- **THEN** the page calls `POST /api/programs`, appends the returned program as a new
  row, and places the name cell in an editable state

#### Scenario: Renaming a program
- **WHEN** the admin user edits a program row's name and clicks that row's `Save`
  button
- **THEN** the page calls `PATCH /api/programs/:id` with the new name and updates the
  row on success

#### Scenario: Save button hidden with no unsaved changes
- **WHEN** a program row's name has not been edited since the last successful save
- **THEN** that row's `Save` button is not shown

#### Scenario: Save button appears on edit
- **WHEN** the admin user edits a program row's name to a value different from the
  last-saved value
- **THEN** that row's `Save` button becomes visible

#### Scenario: Save button hides again after a successful save
- **WHEN** a `Save` click succeeds
- **THEN** that row's `Save` button is hidden again

#### Scenario: Deleting a program
- **WHEN** the admin user clicks a program row's `Delete` button
- **THEN** the page calls `DELETE /api/programs/:id` and removes the row from the list
  on success

#### Scenario: Save rejects a name that fails the length bound client-side
- **WHEN** the admin user clicks `Save` with a name outside the 5-100 character bound
- **THEN** the page shows a validation error on that row and does not call
  `PATCH /api/programs/:id`

#### Scenario: Save surfaces a server-side rejection without crashing
- **WHEN** a `PATCH /api/programs/:id` call made by `Save` returns `400`
- **THEN** the page shows the returned error on that row and remains usable -- it does
  not throw an unhandled exception
