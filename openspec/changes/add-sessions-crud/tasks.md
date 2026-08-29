## 1. D1 schema

- [x] 1.1 Write `migrations/0003_create_sessions.sql`: `sessions` table (`id INTEGER
      PRIMARY KEY AUTOINCREMENT`, `session_id TEXT NOT NULL UNIQUE`, `program_id TEXT
      NOT NULL REFERENCES programs(program_id) ON DELETE CASCADE`, `name TEXT NOT
      NULL`, `created_at TEXT NOT NULL`, `updated_at TEXT NOT NULL`) plus
      `CREATE UNIQUE INDEX idx_sessions_program_name_nocase ON sessions (program_id,
      name COLLATE NOCASE)`.
- [x] 1.2 Confirm (via the Miniflare-backed vitest suite, per design.md's Open
      Questions) whether the D1 binding enforces `ON DELETE CASCADE` as-is. If not,
      enable `PRAGMA foreign_keys = ON` for the binding, or fall back to the Worker
      issuing an explicit `DELETE FROM sessions WHERE program_id = ?` before deleting
      the program row -- resolve this before task group 3. **Confirmed**: a scratch
      vitest test verified D1's Miniflare binding enforces `ON DELETE CASCADE` without
      any extra `PRAGMA` -- no fallback needed.

## 2. Worker: generalize shared helpers (red-green-refactor against existing tests)

- [x] 2.1 `src/ids.js`: extract `generateId(prefix)` and `isValidId(value, prefix)`;
      reimplement `generateProgramId`/`isValidProgramId` as thin wrappers; add
      `generateSessionId`/`isValidSessionId` wrappers using `SNN-`. Update
      `src/ids.test.js` for the shared helpers and both wrappers.
- [x] 2.2 `src/errors.js`: change `uniqueConstraintColumns(err)` to
      `uniqueConstraintColumns(err, table)`, parameterizing the `\btable\.(\w+)` regex.
      Updated both existing call sites in `src/programs.js` (create and rename, not
      just the one the task description named).
- [x] 2.3 `src/validation.js`: generalize `validateProgramName` into a shared
      length-bound check reusable for session names (either a `validateName(value)`
      both wrap, or an exported constant pair for min/max). Update
      `src/validation.test.js`. Added `validateName` as the shared implementation with
      `validateProgramName`/`validateSessionName` as wrappers, plus tests for the new
      `validateSessionName` wrapper.
- [x] 2.4 Run the full existing vitest suite and confirm no regressions from 2.1-2.3
      before writing any session-specific code. **99/99 passing.**

## 3. Worker: sessions feature

- [x] 3.1 Write failing vitest cases in a new `src/sessions.test.js` for
      `listSessions`, `createSession`, `renameSession`, `deleteSession` (mirroring
      `src/programs.js`'s test shape), covering: list scoped to program, create with
      generated ID, per-program duplicate-name conflict, rename, delete, and the
      program-not-found case. **Adjusted**: `programs.js` itself has no dedicated unit
      test file -- its logic is only covered through `index.test.js`'s HTTP-level
      integration tests. Followed that actual convention instead of introducing a new
      per-module test file: session route/business-logic coverage lives in
      `index.test.js`, confirmed red (14 failing) before task 3.4's implementation.
- [x] 3.2 Implement `src/sessions.js` (`listSessions(db, programId)`,
      `createSession(db, programId, name)`, `renameSession(db, programId, id, name)`,
      `deleteSession(db, programId, id)`) to pass 3.1's tests. Also added
      `programExists(db, programId)` (needed for the collection routes' 404 case) and
      generalized `DuplicateNameError` to take an `entityLabel` param (was hardcoded to
      "program") so sessions get a correct conflict message.
- [x] 3.3 Write failing vitest cases in `src/index.test.js` for the new routes:
      `GET/POST /api/programs/:programId/sessions`,
      `PATCH/DELETE /api/programs/:programId/sessions/:id`, including 404s for an
      unknown `:programId` and for a session `:id` that belongs to a different
      program.
- [x] 3.4 Add route handling in `src/index.js` for the nested session routes (extend
      the existing segment-based routing), reusing `jsonResponse`/`withCors`/error
      mapping patterns from the programs handlers. Confirm 3.3 passes.
- [x] 3.5 Write a failing vitest case verifying that deleting a program with sessions
      also removes its sessions (per the `programs` capability's new cascade
      scenario). Confirm it passes once task group 1's schema/FK decision is applied.

## 4. Domain: SessionId

- [x] 4.1 Write a failing xUnit test in `Trainfree.Domain.Tests` for `SessionId.Parse`/
      `TryParse` (valid `SNN-` + 6-char body, invalid prefix, invalid length, invalid
      alphabet), mirroring `ProgramIdTests`. Confirmed red (compile error) before 4.2.
- [x] 4.2 Implement `src/Trainfree.Domain/Ids/SessionId.cs` (same shape as
      `ProgramId.cs`, `SNN-` prefix) to pass 4.1. **36/36 Domain tests passing**,
      CSharpier-clean.

## 5. Blazor: session API client and outcome types

- [x] 5.1 Write failing bUnit/xUnit tests for a `SessionsApiClient` (analogous to
      `ProgramsApiClient`/`ProgramsApiClient.Logging.cs`) covering
      `GetSessionsAsync(ProgramId)`, `CreateSessionAsync(ProgramId, name)`,
      `RenameSessionAsync(ProgramId, SessionId, name)`,
      `DeleteSessionAsync(ProgramId, SessionId)` against the nested routes. Mirrored
      `ProgramsApiClientTests`'s exact coverage shape (no direct `GetSessionsAsync`
      test either, matching that `GetProgramsAsync` isn't unit-tested there -- it's
      covered at the page level instead, task group 6). Confirmed red (compile error)
      before 5.2.
- [x] 5.2 Implement `Admin/SessionSummary.cs`, `Admin/CreateSessionOutcome.cs`,
      `Admin/RenameSessionOutcome.cs`, `Admin/DeleteSessionOutcome.cs`,
      `Admin/ISessionsApiClient.cs`, `Admin/SessionsApiClient.cs` +
      `SessionsApiClient.Logging.cs` to pass 5.1. Register the client in DI alongside
      `IProgramsApiClient` (`Program.cs`). **45/45 Admin tests, 36/36 Domain tests
      passing**, CSharpier-clean.

## 6. Blazor: nested session rows in Programs.razor

- [x] 6.1 Write failing bUnit tests for `Programs.razor` covering: sessions render
      nested under their program on load, add/rename/revert/delete session rows,
      client-side name-length validation blocking `Save`, and server-error surfacing
      on a `400`/`409` response -- mirroring the existing `ProgramRow` test coverage
      pattern. Added to the existing `ProgramsPageTests.cs` (natural home, same as
      `programs.js`'s tests living in `index.test.js` rather than a new file).
      Confirmed 9/9 new tests red before 6.2/6.3.
- [x] 6.2 Extend `Programs.razor`'s `@code` block with a `SessionRow` class (same
      working/saved-value/`IsDirty` shape as `ProgramRow`) and load each program's
      sessions in `OnInitializedAsync` (or lazily per program -- pick one and note it
      doesn't change the spec's observable behavior). Chose eager loading (one
      `GetSessionsAsync` call per program during `OnInitializedAsync`) since there's no
      collapse/expand state yet to defer it for.
- [x] 6.3 Add markup: an `Add Session` action per program row, and session rows
      rendered as additional indented `<tr>`s beneath their program (no chevron/
      collapse state -- plain always-visible nesting per design.md). Reuse the
      existing Save/Revert/Delete button markup pattern with `data-testid` suffixed by
      the session's ID (`session-name-input-`, `session-save-`, `session-revert-`,
      `session-delete-`, `session-name-error-`).
- [x] 6.4 Confirm all of 6.1 passes. **25/25 in `ProgramsPageTests`**; also updated
      `MainLayoutTests` to register the now-required `ISessionsApiClient` in its bUnit
      DI container (it renders the real `Programs` component). Full Admin suite:
      **54/54 passing**, CSharpier-clean.

## 7. Verification

- [ ] 7.1 Run the full `Trainfree.AdminApi` vitest suite and the full
      `Trainfree.Admin`/`Trainfree.Domain` .NET test suites; confirm green.
- [ ] 7.2 Manually verify locally (per CLAUDE.md's port-9999 dev loop): create a
      program, add sessions, rename one, delete one, delete the program and confirm
      its sessions are gone too.
- [ ] 7.3 Confirm CSharpier formatting and any analyzer warnings are clean on the new
      C# files.
