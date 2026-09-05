## MODIFIED Requirements

### Requirement: List programs
The system SHALL provide `GET /api/programs`, returning all programs in creation
order. When two or more programs share the same `created_at` timestamp, the system
SHALL break the tie by `id` ascending, so the order is deterministic across repeated
calls.
**Rationale**: `created_at` is a millisecond-resolution string set via
`new Date().toISOString()`; two inserts within the same millisecond tie on that
column, and SQLite gives no ordering guarantee between tied rows.

#### Scenario: No programs exist
- **WHEN** a client calls `GET /api/programs` and the `programs` table is empty
- **THEN** the Worker responds `200` with an empty JSON array

#### Scenario: Programs exist
- **WHEN** a client calls `GET /api/programs` and programs exist
- **THEN** the Worker responds `200` with a JSON array of programs ordered by
  `created_at` ascending

#### Scenario: Two programs share the same created_at
- **WHEN** a client calls `GET /api/programs` and two programs have an identical
  `created_at` value
- **THEN** the Worker responds `200` with those two programs ordered by `id`
  ascending relative to each other, and this order is the same on every call

## Decisions

- Tiebreak on the table's internal `id` column, qualified as `<table>.id` in the
  `ORDER BY` clause (e.g. `programs.id ASC`, `phases.id ASC`), not the unqualified
  `id` output alias. `SELECT_COLUMNS` in both `programs.js` and `phases.js` aliases
  the public id column (`program_id`/`phase_id`) `AS id`; an unqualified `id` in
  `ORDER BY` resolves to that output alias, not the table's row id, silently
  defeating the tiebreak. `sessions.js` already qualifies its tiebreak this way
  (`sessions.id ASC`) since #43 - this change follows the same pattern rather than
  introducing a new one.
- `listSessions` is out of scope: it already tiebreaks on `sessions.id ASC` (added in
  #43, before the same bug was introduced in the categories/phases and programs list
  queries). Issue #53's mention of `sessions.js` as pre-existing was stale by the time
  this change was picked up; re-verified against current `sessions.js` before writing
  this spec.
- `listCategories` from issue #53 is `listPhases` today: the `Category` entity was
  renamed to `Phase` in #60, after the issue was filed but before this change. No
  separate categories capability exists to modify.

## Requirement coverage

Anchor: issue #53 (listPrograms/listSessions/listCategories have no tiebreaker for
creation-order ties)

| # | Anchor requirement | Covered by |
|---|--------------------|-----------|
| 1 | `listPrograms`, `listSessions`, `listCategories` all tiebreak on the row's internal `id` when `created_at` matches | `listPrograms`: Req "List programs" (this file). `listPhases` (renamed from `listCategories`): Req "List phases" (`specs/phases/spec.md`). `listSessions`: not covered - already correct since #43, see Decisions |
| 2 | A route-level test per table inserts two rows with an identical `created_at` and asserts deterministic order | Scenario "Two programs share the same created_at" (this file) and phases' equivalent scenario (`specs/phases/spec.md`); no new test for sessions, see Decisions |
