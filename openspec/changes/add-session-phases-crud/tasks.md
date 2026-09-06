## 1. D1 schema

- [ ] 1.1 Add migration creating `session_phases` (`id` autoincrement PK, `session_phase_id`
      unique surrogate key, `session_id` FK to `sessions(session_id) ON DELETE CASCADE`,
      `phase_id` FK to `phases(phase_id)` with no `ON DELETE` action (default restrict --
      the guard in tasks 2.x is the primary defense, this is the DB-level backstop),
      `created_at`), mirroring `0003_create_sessions.sql`'s shape. Verify with
      `wrangler d1 migrations apply trainfree_db --local` that it applies cleanly against a
      fresh local DB.

## 2. Worker: session-phases module (TDD)

- [ ] 2.1 Add `generateSessionPhaseId`/`isValidSessionPhaseId` to `ids.js` with a `SPH-`
      prefix, following `generatePhaseId`/`isValidPhaseId`. Verify with a unit test in
      `ids.test.js` asserting the generated ID shape.
- [ ] 2.2 Write a failing test in a new `session-phases.test.js` for `sessionExists(db,
      programId, sessionId)` (mirrors `programExists`), confirm it fails, then implement it
      in a new `session-phases.js`.
- [ ] 2.3 Write a failing test for `listSessionPhases(db, sessionId)` returning
      `{ id, sessionId, phaseId, createdAt }` rows ordered by `created_at` ascending then
      `session_phases.id` ascending (tiebreak, matching `sessions.js`'s
      `LIST_SESSIONS_QUERY` comment), confirm it fails, then implement it.
- [ ] 2.4 Write failing tests for `createSessionPhase(db, sessionId, phaseId)`: creates a row
      with a generated ID and returns it; retries on a generated-ID collision (same
      `MAX_ID_GENERATION_ATTEMPTS` pattern as `createSession`); allows a second row with the
      same `(sessionId, phaseId)` pair (no uniqueness constraint, per the spec's Decision).
      Confirm each fails, then implement.
- [ ] 2.5 Write a failing test for `deleteSessionPhase(db, sessionId, id)` returning whether a
      row was deleted, scoped so an `id` belonging to a different session returns `false`.
      Confirm it fails, then implement it.

## 3. Worker: Phase delete guard (TDD)

- [ ] 3.1 Add a `PhaseInUseError` to `errors.js` alongside `DuplicateNameError`.
- [ ] 3.2 Write a failing test in `phases.test.js` (new file, following the pattern of the
      other Worker unit tests) asserting `deletePhase` throws `PhaseInUseError` when at
      least one `session_phases` row references that `phase_id`, and makes no change.
      Confirm it fails, then update `deletePhase` in `phases.js` to check usage
      (`SELECT 1 FROM session_phases WHERE phase_id = ?`) before deleting and throw
      accordingly.
- [ ] 3.3 Write a failing test confirming `deletePhase` still deletes and returns `true` for
      an unreferenced phase (regression coverage for the existing unconditional-delete
      path). Confirm it fails for the right reason if the guard query has a bug, then
      confirm it passes against the fixed implementation.

## 4. Worker: routing

- [ ] 4.1 Add `handleSessionPhasesCollection`/`handleSessionPhaseResource` to `index.js`,
      validating `programId`/`sessionId` via `programExists`/`sessionExists` (404 if either
      is missing) before dispatching GET/POST or DELETE, following
      `handleSessionsCollection`/`handleSessionResource`'s shape. `POST` validates `phaseId`
      is present and references an existing phase (400 otherwise, reusing a phase-exists
      check rather than relying on the DB-level FK error).
- [ ] 4.2 Extend `routePrograms`'s segment-length routing to recognize
      `/api/programs/:id/sessions/:sessionId/phases[/:id]` (segments length 6 or 7) in
      addition to the existing sessions route, per the comment above `routePrograms`.
- [ ] 4.3 Update `handlePhaseResource`'s `DELETE` branch to catch `PhaseInUseError` and
      respond `409` with a JSON error body, matching the existing `DuplicateNameError` ->
      `409` mapping pattern used elsewhere in `index.js`.
- [ ] 4.4 Write `index.test.js` integration tests (real D1 via Miniflare, no mocking) covering:
      list/create/delete for `/phases` under a session; 404 for a missing program or
      session; 400 for a missing/invalid `phaseId`; 404 deleting a session phase under the
      wrong session; a session delete cascading to its session phases; and the `/api/phases/:id`
      `DELETE` returning `409` when in use vs `204` when not. Verify with
      `npm test` (vitest) passing.

## 5. Domain: SessionPhaseId

- [ ] 5.1 Add `SessionPhaseId` to `Trainfree.Domain/Ids/`, following `PhaseId.cs`'s shape
      (`SPH-` prefix). Verify with a unit test in `Trainfree.Domain.Tests` covering valid
      parse, invalid prefix, and invalid body-length rejection, matching `PhaseId`'s
      existing test coverage.

## 6. Blazor: SessionPhasesApiClient (TDD)

- [ ] 6.1 Define `SessionPhaseSummary`, `CreateSessionPhaseOutcome`
      (`CreateSessionPhaseSucceeded`/`CreateSessionPhaseFailed`), and
      `DeleteSessionPhaseOutcome` (`DeleteSessionPhaseSucceeded`/`DeleteSessionPhaseFailed`)
      as outcome types in `Trainfree.Admin/Admin/`, mirroring `ISessionsApiClient`'s
      `SessionSummary`/`CreateSessionOutcome`/`DeleteSessionOutcome` shapes (no `Rename`
      outcome -- this capability has no rename route).
- [ ] 6.2 Write failing `SessionPhasesApiClient` tests (fake `HttpMessageHandler`, per
      CLAUDE-baseline.md's `MockSendAsync` pattern) for `GetSessionPhasesAsync`,
      `CreateSessionPhaseAsync` (success, 400, 404, and transport/parse exception ->
      `CreateSessionPhaseFailed`), and `DeleteSessionPhaseAsync` (success, 404, transport
      exception). Confirm each fails, then implement `ISessionPhasesApiClient`/
      `SessionPhasesApiClient` against `/api/programs/:programId/sessions/:sessionId/phases`.
- [ ] 6.3 Register `ISessionPhasesApiClient`/`SessionPhasesApiClient` in DI alongside the
      other API clients (wherever `ISessionsApiClient` is registered).
- [ ] 6.4 Write a failing test asserting `PhasesApiClient.DeletePhaseAsync` surfaces a `409`
      response as a `DeletePhaseFailed` outcome (it likely already treats any non-2xx as
      failure generically -- confirm existing coverage, add a `409`-specific case only if
      the current test suite doesn't already exercise it), then confirm it passes.

## 7. Blazor: nested phase rows on the Programs page (TDD)

- [ ] 7.1 Write failing bUnit tests (NSubstitute-faked `ISessionPhasesApiClient`) for:
      loading a session's phases and rendering rows nested under it; a session's phases
      failing to load showing a per-row error while other rows still render; a session
      starting expanded showing its phases; collapsing/expanding a session's chevron
      toggling its phase rows without a re-fetch; collapsing one session leaving others
      unaffected.
- [ ] 7.2 Confirm each test fails, then extend `Programs.razor`/`Programs.razor.Logging.cs`
      (or extract a component if the file is getting unwieldy) to add the session-level
      chevron, fetch and render phase rows on expand, and resolve each phase row's display
      name by matching `phaseId` against the already-loaded phases list (per the spec's
      Decision -- fetch `GET /api/phases` once for this purpose, not per row).
- [ ] 7.3 Write failing bUnit tests for the `Add Phase` flow: clicking it shows a plain
      dropdown of the phase library; selecting a phase calls `CreateSessionPhaseAsync` and
      appends the returned row; an empty phase library shows guidance to create one on the
      `Phases` page first and makes no API call. Confirm each fails, then implement.
- [ ] 7.4 Write failing bUnit tests for deleting a session phase (row removed on success;
      server error shown on the row without crashing the page). Confirm each fails, then
      implement.
- [ ] 7.5 Run the full bUnit suite for `Trainfree.Admin.Tests` and confirm green.

## 8. Manual verification

- [ ] 8.1 Run `wrangler dev` (port 9999) and the Blazor dev server locally; in the browser,
      add a session phase, confirm it renders under its session, delete it, and confirm
      attempting to delete an in-use phase from the `Phases` page surfaces the `409` error
      instead of silently failing.
- [ ] 8.2 Run `dotnet test Trainfree.slnx --configuration Release` and the Worker's
      `npm test` and confirm both suites are green before requesting review.
