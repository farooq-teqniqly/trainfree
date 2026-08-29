## Context

`Trainfree.AdminApi` (the Worker) currently owns one entity, `Program`, with a flat
`GET/POST/PATCH/DELETE /api/programs[/:id]` surface (`src/programs.js`, routed in
`src/index.js`). ID generation (`src/ids.js`), unique-name conflict detection
(`src/errors.js`'s `uniqueConstraintColumns`), and validation (`src/validation.js`) are
all currently hardcoded to the `programs` table/`ProgramId` shape -- this is the first
slice that adds a second entity, so those three modules need to generalize rather than
be copy-pasted with `session` substituted in.

`Trainfree.Admin`'s `Programs.razor` renders one flat, unstyled `<table>` of program
rows (`ProgramRow` with `IsDirty`/`Save`/`Revert`). This slice nests session rows under
their program in that same table, ahead of `restyle-admin-shell` (slice 4), which owns
turning it into the collapsible-indented spreadsheet look.

## Goals / Non-Goals

**Goals:**
- Add per-program session CRUD end to end (D1 -> Worker -> Blazor), TDD'd on both
  stacks.
- Generalize the Worker's ID-generation, duplicate-name detection, and validation
  helpers so they serve both `programs` and `sessions` without duplicating logic per
  entity.
- Cascade-delete a program's sessions at the DB level.

**Non-Goals:**
- Any visual redesign of the admin page (chevrons, indentation styling, sidebar nav) --
  `restyle-admin-shell` (slice 4).
- Session reordering/position field -- deferred to `add-program-categories-exercises-crud`
  (slice 7), which introduces the first real drag/reorder need.
- Categories or exercises on a session -- slices 5-7.

## Decisions

**Nested routes, not a flat filtered collection.** `GET/POST /api/programs/:programId/sessions`
and `PATCH/DELETE /api/programs/:programId/sessions/:id`, mirroring the segment-count
routing style already in `index.js` (`segments[0] === "api"`, etc.) rather than a flat
`/api/sessions?programId=`. A session has no meaning outside its program, and nesting
gives a natural `404` when `:programId` doesn't exist versus a silently-empty list.

**`uniqueConstraintColumns` and ID generation become table-parameterized.**
`uniqueConstraintColumns(err)` currently regexes for `\bprograms\.(\w+)`. It becomes
`uniqueConstraintColumns(err, table)` so `sessions.js` can call
`uniqueConstraintColumns(err, "sessions")`. Likewise `ids.js`'s `generateProgramId`
becomes a shared `generateId(prefix)` helper; `generateProgramId` and the new
`generateSessionId` become thin wrappers (`() => generateId("PRG-")` /
`() => generateId("SNN-")`) so the Crockford-base32 body generation isn't duplicated.
`isValidProgramId` similarly factors into a shared `isValidId(value, prefix)`.

**Validation becomes name-only and reusable.** `validateProgramName` already just
checks the 4-100 trimmed-length bound with no program-specific behavior; it's renamed
`validateEntityName` (or kept as `validateProgramName` with `validateSessionName` as a
wrapper) so both entities share one bound-check implementation rather than two copies
drifting apart.

**Per-program uniqueness via a composite index, not a global one.** `programs` uses
`CREATE UNIQUE INDEX idx_programs_name_nocase ON programs (name COLLATE NOCASE)`. For
sessions the uniqueness scope is the owning program, so:
`CREATE UNIQUE INDEX idx_sessions_program_name_nocase ON sessions (program_id, name COLLATE NOCASE)`.
Two different programs can each have a "Monday Lower Body" session; the same program
cannot have two.

**Cascade delete via `ON DELETE CASCADE` FK, not application-level delete-then-delete.**
`sessions.program_id REFERENCES programs(program_id) ON DELETE CASCADE`. Deleting a
program's row lets SQLite/D1 remove its sessions atomically, rather than the Worker
issuing a separate `DELETE FROM sessions WHERE program_id = ?` first (a second query
that could race or partially fail). D1/SQLite requires `PRAGMA foreign_keys = ON` to
enforce cascade -- confirm this is already set for the D1 binding, or set it, as part of
this slice's Worker changes (D1 does not enable FK enforcement by default the way some
SQLite embedders do).

**Session ID prefix `SNN-`**, structurally identical to `ProgramId` (6 Crockford
base32 chars from the same `ABCDEFGHJKMNPQRSTVWXYZ23456789` alphabet). Chosen over
`SES-` to avoid visual confusion with the future workout-runner `Set` entity once both
prefixes appear together in the workout app's history views.

**Blazor: sessions nested inline under their program row, not a separate page/route.**
Consistent with the roadmap's framing of the admin app as one spreadsheet-shaped page
that grows rows/columns slice by slice (`restyle-admin-shell` confirms this by later
adding indentation to the *same* table, not moving sessions to their own page).
`Programs.razor` grows a per-program `List<SessionRow>` alongside the existing
`ProgramRow`, with its own add/rename/delete calls against a new `ISessionsApiClient`
(scoped by `programId`, mirroring `IProgramsApiClient`'s shape). Session rows render as
additional `<tr>`s directly under their program's row with an indent (e.g. a leading
`ms-4` on the name cell) -- no expand/collapse state, since slice 4 owns that
interaction.

## Risks / Trade-offs

- **D1 FK cascade support** -> D1 is SQLite-based and supports `ON DELETE CASCADE`, but
  only when foreign key enforcement is turned on for the connection; if it turns out
  the Worker's D1 binding does not enforce FKs by default, fall back to the Worker
  issuing an explicit `DELETE FROM sessions WHERE program_id = ?` before deleting the
  program row, inside the same request (D1 does not expose multi-statement
  transactions across separate `prepare().run()` calls the way a server RDBMS
  connection would, so this must be verified against the real Miniflare-backed test
  suite, not assumed).
- **Refactoring `uniqueConstraintColumns`/`ids.js` touches slice-1 code** -> covered by
  slice 1's existing vitest suite; any behavior change there is caught by re-running
  `src/index.test.js` and `src/validation.test.js` before adding session-specific
  tests, not just by adding new ones.
- **Per-program uniqueness could surprise a user who expects global session-name
  uniqueness** (e.g. copy-pasting a program) -> acceptable per this slice's explicit
  decision; revisit only if product feedback says otherwise.

## Migration Plan

New migration `migrations/0003_create_sessions.sql`, applied automatically by
`wrangler d1 migrations apply` in `deploy.yaml` on the next tag, per CLAUDE.md's deploy
convention. No backfill needed -- `sessions` starts empty. No rollback script beyond
`wrangler d1 migrations` tooling; this is additive (new table + new FK), so it does not
alter existing `programs` data.

## Open Questions

- ~~Confirm D1's FK-cascade enforcement behavior~~ -- **Resolved**: a scratch vitest
  test against the Miniflare-backed D1 binding confirmed `ON DELETE CASCADE` is
  enforced without any extra `PRAGMA foreign_keys` setting. The fallback
  (explicit cascading delete in the Worker) described in Risks is not needed.
