## Why

`Trainfree.Admin` can only manage `Program` rows today (slice 1). A program is just a
name until it has sessions under it -- there is no way yet to record that "Push Pull
Legs" has a "Monday Lower Body" day. Roadmap slice 3 (`add-sessions-crud`) adds the
`Session` entity so a program can be broken into its day-sessions, the next layer the
full spreadsheet (slice 7) will eventually hang categories and exercises off of.

## What Changes

- New `sessions` D1 table: `id` (surrogate key, `SNN-` + 6 Crockford base32 chars,
  mirroring `programs.program_id`), `program_id` (FK to `programs.program_id`,
  `ON DELETE CASCADE`), `name`, `created_at`, `updated_at`.
- New nested Worker routes on `Trainfree.AdminApi`:
  - `GET /api/programs/:programId/sessions` -- list a program's sessions, creation order.
  - `POST /api/programs/:programId/sessions` -- create a session under a program.
  - `PATCH /api/programs/:programId/sessions/:id` -- rename a session.
  - `DELETE /api/programs/:programId/sessions/:id` -- delete a session.
- Session `name` reuses the program name rules: 5-100 characters trimmed, unique
  case-insensitively -- but scoped to its own program, not globally (two different
  programs may each have a "Monday Lower Body" session).
- `Trainfree.Domain.Ids` gains `SessionId`, structurally identical to `ProgramId` but
  with the `SNN-` prefix.
- `Trainfree.Admin`'s `Programs.razor` page renders each program's sessions as indented
  rows directly beneath it (add/rename/delete, same `Save`/`Revert`/dirty-row pattern as
  program rows), using the existing plain `<table>` -- no chevron expand/collapse or
  visual redesign. That styling is `restyle-admin-shell` (slice 4)'s job, which depends
  on this slice existing first.
- Deleting a program deletes its sessions too (DB-level cascade); no confirmation
  prompt beyond what program delete already has, since nothing outside `sessions`
  references a session yet.

## Capabilities

### New Capabilities
- `sessions`: Session identifier format, per-program list/create/rename/delete API,
  per-program name uniqueness, and the admin UI for managing a program's sessions
  inline.

### Modified Capabilities
- `programs`: deleting a program now cascades to delete its sessions (new scenario on
  the existing "Delete a program" requirement); no other program-facing behavior
  changes.

## Impact

- `src/Trainfree.AdminApi`: new `src/sessions.js`, routes added to `src/index.js`, new
  migration `migrations/0003_create_sessions.sql`, vitest coverage.
- `src/Trainfree.Domain`: new `Ids/SessionId.cs`.
- `src/Trainfree.Admin`: new `Admin/SessionSummary.cs`,
  `Admin/{Create,Rename,Delete}SessionOutcome.cs`, `Admin/ISessionsApiClient.cs` +
  implementation, and updates to `Pages/Admin/Programs.razor` (+ its `.Logging.cs`)
  to render and manage nested session rows.
- No changes to `Trainfree.Workout`, `Trainfree.WorkoutApi`, or `Trainfree.Versioning`.
