## MODIFIED Requirements

### Requirement: Create a session phase
The system SHALL provide `POST /api/programs/:programId/sessions/:sessionId/phases` to
add a phase to a session, with a system-generated ID and a required `phaseId` in the
request body referencing an existing phase. This includes the case where the
referenced phase is deleted by a concurrent `DELETE /api/phases/:id` between this
route's existence check and its `INSERT` statement actually landing: the insert SHALL
never surface as an unhandled `500` in that case, only the documented `400`.
**Rationale**: The same canonical phase may be added to a session more than once (e.g.
a session with two separate "Cool Down" blocks), so this route intentionally applies no
uniqueness constraint on `(session_id, phase_id)`. The `phaseId` existence check and the
`INSERT` are not wrapped in a single D1 transaction (this is a single-user app, per
`CLAUDE.md`, so the race window is narrow), so the `INSERT` itself can still hit the
`session_phases.phase_id` foreign key if the referenced phase is deleted in between --
that failure is caught and translated to the same `400` the up-front existence check
produces, rather than propagating as a raw D1 constraint error.

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

#### Scenario: The referenced phase is deleted between the check and the insert
- **WHEN** a client calls `POST /api/programs/:programId/sessions/:sessionId/phases`
  with a `phaseId` that matches an existing phase at the time of the up-front check,
  and a concurrent `DELETE /api/phases/:id` for that same phase removes it before this
  route's `INSERT` statement runs
- **THEN** the Worker's `INSERT` statement fails the `session_phases.phase_id` foreign
  key constraint, the Worker catches that failure, and responds `400` with a JSON error
  body and creates no row -- never an unhandled `500`

## Decisions

- **Catch-and-translate at the write, not a transactional rework (issue #89's Option
  2).** Chose to catch the `session_phases.phase_id` foreign key constraint violation at
  the point of `deletePhase`'s `DELETE` and `createSessionPhase`'s `INSERT`, and
  translate it to the same `409`/`400` each route's up-front check already produces,
  rather than wrapping the check-then-act pair in a D1 `db.batch` to close the race
  itself (issue #89's Option 1). This is the smaller change and directly fixes the
  "500 instead of 400/409" symptom. A `db.batch` would close the race window itself
  instead of just handling its failure mode, and would become the right choice if this
  app stops being single-user (per `CLAUDE.md`) and the race window widens from
  "vanishingly rare" to "occasionally observed in practice."
- **Reuse existing status codes and messages; a new error class only where none already
  fit.** `deletePhase`'s FK failure reuses the existing `PhaseInUseError` (`409`)
  as-is -- the phase became in-use, which is exactly what that error already means, so no
  new type is needed there. `createSessionPhase`'s FK failure has no existing error
  class to reuse (the up-front `phaseExists` check in `handleSessionPhasesCollection`
  returns its `400` directly from a plain `if`, not via a thrown error type), so this
  path introduces `SessionPhaseInvalidPhaseError` carrying the identical message text,
  which the caller maps to the same `400` response the check already returns. Both
  cases mean the same thing to the client -- "that phaseId does not reference an
  existing phase" -- whether the new type exists purely to let `createSessionPhase`
  signal that condition to its caller across a function boundary the check-based
  version didn't need to cross.
- **FK-violation detector takes no `table`/`column` arguments, unlike
  `uniqueConstraintColumns`.** Verified empirically against real D1: a `UNIQUE`
  violation's message names the offending `<table>.<column>` (e.g.
  `UNIQUE constraint failed: programs.name`), but a `FOREIGN KEY` violation's message
  does not -- it is the fixed string `FOREIGN KEY constraint failed: SQLITE_CONSTRAINT`
  regardless of which table or column's foreign key fired. A parameterized detector
  would accept any `table`/`column` pair without actually discriminating between them,
  which is more misleading than an unparameterized one. This is safe here because each
  call site (`deletePhase`'s `DELETE`, `createSessionPhase`'s `INSERT`) has exactly one
  foreign key that can plausibly fail in that statement. It would stop being safe, and
  the detector would need to inspect `err.cause` or a driver-specific field instead of
  the message text, if a single statement could violate more than one foreign key and
  the two failures needed different handling.

## Requirement coverage

Anchor: issue #89 (Concurrent phase delete + session-phase create can crash with an
unhandled FK violation)

| # | Anchor requirement | Covered by |
|---|--------------------|-----------|
| 1 | A concurrent `POST .../phases` racing a `DELETE /api/phases/:id` for the same phase never surfaces as an unhandled `500` | phases: Req "Delete a phase" scenario "A session phase referencing this phase is created between the check and the delete"; session-phases: Req "Create a session phase" scenario "The referenced phase is deleted between the check and the insert" |
| 2 | Add a test exercising the FK-violation path directly, for both directions (delete-wins, create-wins) | Covered by tasks.md, not by spec scenarios themselves -- the two scenarios above define the expected behavior each direction's test must assert |
