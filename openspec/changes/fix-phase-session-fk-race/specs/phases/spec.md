## MODIFIED Requirements

### Requirement: Delete a phase

The system SHALL provide `DELETE /api/phases/:id` to remove a phase, but SHALL reject
the deletion with `409` and make no change when the phase is referenced by at least one
`session_phases` row. This includes the case where the phase becomes referenced by a
concurrent `POST .../phases` (create session phase) between this route's existence
check and its `DELETE` statement actually landing: the delete's own `DELETE` SHALL never
surface as an unhandled `500` in that case, only the documented `409`.
**Rationale**: A phase is a global library entity (not scoped to a single program), so
deleting one that a session already uses would silently orphan that session's
reference; failing the delete is cheaper and safer than a cascade that could reach
across other users' data once multi-user support exists. The existence check and the
`DELETE` are not wrapped in a single D1 transaction (this is a single-user app, per
`CLAUDE.md`, so the race window is narrow), so the `DELETE` itself can still hit the
`session_phases.phase_id` foreign key if a session phase referencing this phase was
created in between -- that failure is caught and translated to the same `409` the
up-front check produces, rather than propagating as a raw D1 constraint error.

#### Scenario: Phase exists and is unused

- **WHEN** a client calls `DELETE /api/phases/:id` for an existing phase referenced by
  no `session_phases` row
- **THEN** the Worker deletes the row and responds `204`

#### Scenario: Phase does not exist

- **WHEN** a client calls `DELETE /api/phases/:id` for an `:id` with no matching phase
- **THEN** the Worker responds `404`

#### Scenario: Phase is used by a session

- **WHEN** a client calls `DELETE /api/phases/:id` for a phase referenced by at least
  one `session_phases` row
- **THEN** the Worker responds `409` with a JSON error body and makes no change

#### Scenario: A session phase referencing this phase is created between the check and the delete

- **WHEN** a client calls `DELETE /api/phases/:id` for a phase with no `session_phases`
  row at the time of the up-front check, and a concurrent
  `POST .../phases` (create session phase) for that same `:id` inserts a `session_phases`
  row before this route's `DELETE` statement runs
- **THEN** the Worker's `DELETE` statement fails the `session_phases.phase_id` foreign
  key constraint, the Worker catches that failure, and responds `409` with a JSON error
  body and makes no change -- never an unhandled `500`
