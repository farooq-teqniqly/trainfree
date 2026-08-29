## MODIFIED Requirements

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
