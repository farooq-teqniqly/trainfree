# session-phases Specification

## Purpose
A session phase is a session's own instance of a canonical `Phase` from the phase
library (e.g. this Monday session's "Warm Up"). It carries no data of its own beyond
which phase it references -- it exists purely to let a session pick phases from the
library, in order, instead of typing phase names as free text. This spec covers a
session phase's externally visible identity, the Worker's nested create/delete API over
the `session_phases` table, its cascade relationship with its parent session, and the
Blazor admin UI that manages it nested under a session row.

## Requirements

### Requirement: Session phase identifier format
Each session phase SHALL be identified externally by a surrogate key in the form
`SPH-` followed by 6 Crockford base32 characters (e.g. `SPH-7K2QXM`), never by the
table's internal auto-incrementing row key. All API routes, request bodies, and
response bodies use this surrogate key as `id`.
**Rationale**: Matches the surrogate-key convention already used by `programs`,
`sessions`, `phases`, and `exercises`, so no capability's IDs are guessable or leak the
internal row key.

#### Scenario: Generated ID shape
- **WHEN** the Worker creates a new session phase
- **THEN** the generated `id` matches `SPH-` followed by exactly 6 characters from the
  alphabet `ABCDEFGHJKMNPQRSTVWXYZ23456789`

### Requirement: List a session's phases
The system SHALL provide `GET /api/programs/:programId/sessions/:sessionId/phases`,
returning all session phases belonging to that session in creation order, each
including the `phaseId` it references.
**Rationale**: A session phase has no name of its own; the admin UI resolves each
row's display name by matching `phaseId` against the phase library it already loads
to populate the picker, so the Worker does not need to join and embed a `phaseName`.

#### Scenario: Session has no phases
- **WHEN** a client calls `GET /api/programs/:programId/sessions/:sessionId/phases` for
  an existing session with no phases
- **THEN** the Worker responds `200` with an empty JSON array

#### Scenario: Session has phases
- **WHEN** a client calls `GET /api/programs/:programId/sessions/:sessionId/phases` for
  a session with phases
- **THEN** the Worker responds `200` with a JSON array of that session's phases ordered
  by `created_at` ascending, excluding phases belonging to other sessions

#### Scenario: Session does not exist under that program
- **WHEN** a client calls `GET /api/programs/:programId/sessions/:sessionId/phases` for
  a `:sessionId` with no matching session under `:programId` (including a `:sessionId`
  that exists but belongs to a different program)
- **THEN** the Worker responds `404`

### Requirement: Create a session phase
The system SHALL provide `POST /api/programs/:programId/sessions/:sessionId/phases` to
add a phase to a session, with a system-generated ID and a required `phaseId` in the
request body referencing an existing phase.
**Rationale**: The same canonical phase may be added to a session more than once (e.g.
a session with two separate "Cool Down" blocks), so this route intentionally applies no
uniqueness constraint on `(session_id, phase_id)`.

#### Scenario: Valid phaseId provided
- **WHEN** a client calls `POST /api/programs/:programId/sessions/:sessionId/phases`
  for an existing session with a JSON body whose `phaseId` matches an existing phase
- **THEN** the Worker creates a `session_phases` row with a generated ID, that
  `session_id`, the given `phase_id`, and a current `created_at`, and responds `201`
  with the created session phase

#### Scenario: Session does not exist under that program
- **WHEN** a client calls `POST /api/programs/:programId/sessions/:sessionId/phases`
  for a `:sessionId` with no matching session under `:programId`
- **THEN** the Worker responds `404` and creates no row

#### Scenario: phaseId is missing or does not reference an existing phase
- **WHEN** a client calls `POST /api/programs/:programId/sessions/:sessionId/phases`
  with a missing `phaseId`, or a `phaseId` that matches no phase
- **THEN** the Worker responds `400` with a JSON error body and creates no row

#### Scenario: The same phase is added to a session twice
- **WHEN** a client calls `POST /api/programs/:programId/sessions/:sessionId/phases`
  with a `phaseId` that already has a session phase row under that same session
- **THEN** the Worker creates a second, independent session phase row and responds
  `201`

### Requirement: A session phase cannot be renamed
The system SHALL NOT provide a route to change which phase an existing session phase
references.
**Rationale**: A session phase carries no data beyond its `phaseId`; changing that
reference is indistinguishable from deleting the row and creating a new one, so no
`PATCH` route exists for this capability.

#### Scenario: No update route exists
- **WHEN** a client calls any HTTP method other than `GET`, `POST`, or `DELETE` on
  `/api/programs/:programId/sessions/:sessionId/phases[/:id]`
- **THEN** the Worker responds `404` or `405` -- the route is simply not registered

### Requirement: Delete a session phase
The system SHALL provide
`DELETE /api/programs/:programId/sessions/:sessionId/phases/:id` to remove a session
phase unconditionally -- no other table references `session_phases` yet, since the
`ProgramExercise` join that would make a session phase "used" is deferred to a later
change.

#### Scenario: Session phase exists
- **WHEN** a client calls
  `DELETE /api/programs/:programId/sessions/:sessionId/phases/:id` for an existing
  session phase under that session
- **THEN** the Worker deletes the row and responds `204`

#### Scenario: Session phase does not exist under that session
- **WHEN** a client calls
  `DELETE /api/programs/:programId/sessions/:sessionId/phases/:id` for an `:id` with no
  matching session phase under `:sessionId` (including an `:id` that exists but belongs
  to a different session)
- **THEN** the Worker responds `404`

### Requirement: Deleting a session cascades to its session phases
When a session is deleted, the system SHALL also delete every session phase belonging
to it.
**Rationale**: A session phase has no independent existence or meaning outside its
parent session, matching the existing cascade from a deleted program to its sessions.

#### Scenario: Deleting a session with phases
- **WHEN** the admin user deletes a session that has one or more session phases
- **THEN** `DELETE /api/programs/:programId/sessions/:id` succeeds and every session
  phase that belonged to that session is also removed

### Requirement: Admin session phase rows nested under their session
The Blazor admin page SHALL display each session's phases as rows nested beneath that
session's row, and allow adding and deleting them without a full page reload, using the
same chevron expand/collapse pattern already used for a program's sessions. Each
session's phases SHALL be independently expandable/collapsible; a session starts
expanded, matching the existing default for a program's sessions.
**Rationale**: Reuses the same nesting and default-expanded pattern the Sessions page
already established for programs, so the spreadsheet's visual grammar stays consistent
one level deeper.

#### Scenario: Page loads with existing session phases
- **WHEN** the admin page loads and a session has session phases
- **THEN** it calls `GET /api/programs/:programId/sessions/:sessionId/phases` for that
  session and renders one row per returned session phase, nested beneath that session's
  row, showing the name of the phase each row references

#### Scenario: One session's phases fail to load
- **WHEN** the admin page loads and
  `GET /api/programs/:programId/sessions/:sessionId/phases` fails for one session
- **THEN** that session's row still renders (with no phases) alongside a per-row
  load-failed message, and every other session's row and phases still render normally

#### Scenario: Adding a phase to a session
- **WHEN** the admin user clicks a session's `Add Phase` action and selects a phase
  from the plain dropdown of the phase library (no search box, no inline "New
  phase..." shortcut)
- **THEN** the page calls `POST /api/programs/:programId/sessions/:sessionId/phases`
  with that `phaseId` and appends the returned session phase as a new row under that
  session, showing the selected phase's name

#### Scenario: The phase library is empty
- **WHEN** the admin user clicks a session's `Add Phase` action and no phases exist in
  the library
- **THEN** the page shows guidance to create a phase on the `Phases` page first, and
  makes no `POST` call

#### Scenario: Deleting a session phase
- **WHEN** the admin user clicks a session phase row's `Delete` button
- **THEN** the page calls
  `DELETE /api/programs/:programId/sessions/:sessionId/phases/:id` and removes the row
  from the list on success

#### Scenario: Delete surfaces a server-side failure without crashing
- **WHEN** a `DELETE /api/programs/:programId/sessions/:sessionId/phases/:id` call
  returns a non-2xx response
- **THEN** the page shows the returned error on that row and remains usable -- it does
  not throw an unhandled exception

#### Scenario: A session starts expanded
- **WHEN** the admin page loads
- **THEN** every session's phases are visible by default (no session starts collapsed),
  matching the existing default for a program's sessions

#### Scenario: Collapsing a session hides its phases
- **WHEN** the admin user clicks an expanded session's chevron
- **THEN** that session's phase rows stop rendering, the `Add Phase` action remains
  available on the session row, and the chevron's orientation reflects the collapsed
  state

#### Scenario: Expanding a collapsed session shows its phases again
- **WHEN** the admin user clicks a collapsed session's chevron
- **THEN** that session's phase rows render again, in the same order as before
  collapsing, with no re-fetch from the API

#### Scenario: Collapsing one session does not affect others
- **WHEN** the admin user collapses one session with multiple sessions loaded
- **THEN** every other session's expanded/collapsed state is unchanged
