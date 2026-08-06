## Context

First vertical slice of Trainfree: no D1 schema, Worker routes, or Blazor pages exist
yet. This slice establishes the pattern (migration -> Worker route -> Blazor page) that
every later CRUD slice (sessions, categories, exercises) will repeat.

## Goals / Non-Goals

**Goals:**
- Prove the Worker + D1 + Blazor + deploy pipeline end to end with the smallest entity.
- Establish the D1 migration, Worker route, and Blazor admin page conventions later
  slices will follow.

**Non-Goals:**
- Sessions, categories, exercises, or any nested entity (slices 2-3).
- Client-facing (non-admin) screens.
- Authentication/authorization logic -- Cloudflare Access already gates the whole app.

## Decisions

- **`programs` table**: `id INTEGER PRIMARY KEY AUTOINCREMENT` (internal DB key only --
  fast joins/indexes, never exposed), `program_id TEXT NOT NULL UNIQUE` (public surrogate
  key, the only ID clients see or send), `name TEXT NOT NULL`, `created_at TEXT NOT
  NULL`, `updated_at TEXT NOT NULL` (ISO 8601 strings -- D1/SQLite has no native
  datetime type). All routes and API payloads use `program_id`; `id` never leaves the
  Worker.
- **Surrogate key format**: follows the `PREFIX-BODY` pattern from
  [farooq-teqniqly/trakmark's `Trakmark.Domain/Ids`](https://github.com/farooq-teqniqly/trakmark/tree/main/Trakmark.Domain/Ids) --
  prefix `PRG-` plus a 6-character Crockford base32 body (alphabet `ABCDEFGHJKMNPQRSTVWXYZ23456789`,
  excludes ambiguous `0/O/1/I/L`), e.g. `PRG-7K2QXM`. **The Worker is the sole
  generator** -- it assigns `program_id` on `POST`, following trakmark's `DomainId`/
  `CrockfordBase32` generation logic ported to a JS `ids.js` module. The Blazor client
  never generates an ID; it only receives one from the API. Add a `ProgramId` readonly
  record struct in `Trainfree.Web` mirroring trakmark's `CityId` shape but trimmed to
  what a consumer needs: `Parse`, `TryParse`, `ToString` (round-trips an API-supplied
  value for display and route building) -- no `NewId()`.
- **Worker routes** live under `src/Trainfree.Api`, following the existing convention of
  plain JS modules per CLAUDE.md; no new architectural pattern introduced.
- **Validation**: `name` required, 5-100 characters after trimming leading/trailing
  whitespace, enforced in the Worker before the D1 write (SQLite has no length
  constraint). Reject with `400` and a JSON error body on failure.
- **Blazor admin page**: single page (`/admin` or similar), fetches `GET /api/programs`
  on load, renders one row per program per mockup 11's top-level rows. Inline text edit
  on the name cell triggers `PATCH` on blur; `[+ Program]` triggers `POST` with a
  default name of `"New Program"` (12 chars, satisfies the 5-100 bound) then focuses
  the name cell for immediate rename; `[x]` triggers `DELETE` with no confirmation
  dialog (single-user, low-stakes, consistent with mockup's bare `[x]`).
- **No pagination/sorting**: program counts are small (single user, hand-authored
  programs); return all rows in creation order.

## Risks / Trade-offs

- [Two ID columns per table (internal autoincrement + public surrogate) add a small
  amount of schema/query overhead] -> Mitigation: autoincrement keeps joins/indexes fast
  as later slices add FK-heavy tables (sessions, categories, exercises); the surrogate
  key keeps the public API stable even if rows are ever re-keyed.
- [JS `ids.js` (generation + validation) and C#'s `ProgramId` (validation only) each
  implement the Crockford base32 format independently, no shared code across the
  language boundary] -> Mitigation: format is small and fixed (`PREFIX-6CHARS`); each
  stack's implementation is covered by its own tests; only the Worker generates IDs,
  so there is no risk of the two sides producing divergent values.
- [No confirmation on delete] -> Mitigation: acceptable for a single-user app per
  CLAUDE.md context; revisit only if accidental deletes become a real problem.
