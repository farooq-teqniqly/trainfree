## 1. Foreign-key violation detection helper

- [ ] 1.1 Write a failing test in `errors.test.js` for a new `isForeignKeyViolation(err, table, column)` helper in `errors.js` (parallel to the existing `uniqueConstraintColumns`), asserting it returns `true` for a synthetic D1 `SQLITE_CONSTRAINT` error message naming `session_phases.phase_id` and `false` for an unrelated error, then implement it to pass -- verify with `npx vitest run errors.test.js`
- [ ] 1.2 Confirm the helper's regex is anchored the same way `uniqueConstraintColumns` is (escaped table/column names, case-insensitive) so it isn't fooled by a substring match -- verify by adding a test case with a similarly-named but different table/column pair that must return `false`

## 2. Delete-wins direction: `deletePhase`'s `DELETE` hits the FK constraint

- [ ] 2.1 Write a failing test in `phases.test.js` for `deletePhase` that simulates the race directly against real D1 (per `CLAUDE-baseline.md`'s no-mocking rule): insert a phase with no `session_phases` row (so the up-front in-use check passes), then insert a `session_phases` row referencing it (simulating the concurrent create landing first), then call `deletePhase` and assert it still rejects with `PhaseInUseError` instead of an unhandled D1 constraint error -- verify with `npx vitest run phases.test.js`
- [ ] 2.2 In `phases.js`'s `deletePhase`, wrap the `DELETE FROM phases` call in a try/catch; on catch, use `isForeignKeyViolation(err, "session_phases", "phase_id")` to detect the FK failure and throw `PhaseInUseError(id)` in that case, otherwise rethrow -- verify the test from 2.1 passes
- [ ] 2.3 Run the full `phases.test.js` suite and confirm the existing "phase is used by a session" and "phase exists and is unused" tests still pass unchanged -- verify with `npx vitest run phases.test.js`

## 3. Create-wins direction: `createSessionPhase`'s `INSERT` hits the FK constraint

- [ ] 3.1 Write a failing test in `session-phases.test.js` for `createSessionPhase` that simulates the race directly against real D1: create a phase, then delete it (simulating a concurrent `DELETE /api/phases/:id` landing after `handleSessionPhasesCollection`'s `phaseExists` check but before this call), then call `createSessionPhase` with that now-deleted `phaseId` and assert it rejects with a dedicated error (see 3.2) instead of an unhandled D1 constraint error -- verify with `npx vitest run session-phases.test.js`
- [ ] 3.2 Add a `SessionPhaseInvalidPhaseError` class to `errors.js` (parallel to `PhaseInUseError`), carrying the same message text `handleSessionPhasesCollection`'s existing `400` response already uses ("phaseId is required and must reference an existing phase") -- verify by importing it in the test from 3.1 and asserting `rejects.toBeInstanceOf(SessionPhaseInvalidPhaseError)`
- [ ] 3.3 In `session-phases.js`'s `createSessionPhase`, wrap the `INSERT INTO session_phases` call in a try/catch; on catch, use `isForeignKeyViolation(err, "phases", "phase_id")` to detect the FK failure and throw `SessionPhaseInvalidPhaseError` in that case, otherwise rethrow (existing `session_phase_id` collision-retry logic in the same catch must keep working) -- verify the test from 3.1 passes
- [ ] 3.4 Run the full `session-phases.test.js` suite and confirm the existing id-retry and happy-path tests still pass unchanged -- verify with `npx vitest run session-phases.test.js`

## 4. Wire the new error into the HTTP layer

- [ ] 4.1 In `index.js`'s `handleSessionPhasesCollection`, wrap the `await createSessionPhase(db, sessionId, phaseId)` call in a try/catch that maps `SessionPhaseInvalidPhaseError` to the same `jsonResponse({ error: ... }, 400)` its up-front `phaseExists` check already returns, rethrowing anything else -- verify with a new integration-level test (see 4.2) rather than by inspection alone
- [ ] 4.2 Write a failing test at the `index.js` request-handling level (wherever the existing Worker integration tests for `POST .../phases` live) that races a real `DELETE /api/phases/:id` against a real `POST .../phases` for the same phase and asserts the losing request gets `400`, never `500` -- verify with `npx vitest run` against that test file
- [ ] 4.3 Confirm `handlePhaseResource`'s existing `handleDeleteWithConflict` wiring needs no change, since `deletePhase` now throws the same `PhaseInUseError` type that helper already catches into `409` -- verify by re-running the existing `DELETE /api/phases/:id` integration tests

## 5. Full verification

- [ ] 5.1 Run the complete Worker test suite (`npx vitest run` from `src/Trainfree.AdminApi`) and confirm everything passes
- [ ] 5.2 Manually trace both acceptance criteria from issue #89 against the tests added above and confirm each has a passing test: (a) concurrent create-vs-delete never surfaces `500`, (b) both directions (delete-wins in section 2, create-wins in section 3) have a dedicated test exercising the FK-violation path directly
