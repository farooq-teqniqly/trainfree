## MODIFIED Requirements

### Requirement: Program name length
A program's `name` SHALL be between 4 and 100 characters (inclusive) after trimming
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

### Requirement: Delete a program
The system SHALL provide `DELETE /api/programs/:id` to remove a program. Deleting a
program SHALL also remove all sessions belonging to that program.

#### Scenario: Program exists
- **WHEN** a client calls `DELETE /api/programs/:id` for an existing program
- **THEN** the Worker deletes the row and responds `204`

#### Scenario: Program does not exist
- **WHEN** a client calls `DELETE /api/programs/:id` for an ID with no matching row
- **THEN** the Worker responds `404`

#### Scenario: Deleting a program cascades to its sessions
- **WHEN** a client calls `DELETE /api/programs/:id` for an existing program that has
  one or more sessions
- **THEN** the Worker deletes the program row and all `sessions` rows whose
  `program_id` matches that program, and responds `204`
