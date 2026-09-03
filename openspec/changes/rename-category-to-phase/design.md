## Context

`Category` (slice 5, `add-category-library-crud`, archived and shipped) is a flat, fully
CRUD-able entity: a `categories` D1 table, `/api/categories` Worker routes, and a
`Categories.razor` admin page following the same working/saved-value dirty-row pattern as
`Programs`/`Sessions`. Nothing references it yet -- `SessionCategory` (the join that will
attach a category to a session) is slice 7, not yet built. This is the last point before
that dependency exists where a rename touches only the entity itself, not a foreign key
or a picker UI built on top of it.

There is no production data (v0.1 hasn't shipped), so this is a pure rename, not a data
migration.

## Goals / Non-Goals

**Goals:**
- Rename `Category` -> `Phase` everywhere: domain ID type, D1 table, Worker routes,
  Admin UI, tests, OpenSpec capability, and docs/mockups.
- Preserve every existing behavior (ID format shape, name-length bounds, uniqueness,
  unconditional delete, dirty-row UI pattern) unchanged under the new name.
- Land this as its own change, separate from slice 7's `add-program-categories-exercises-crud`.

**Non-Goals:**
- No new capability (usage guard, ordering, session linkage) -- that's slice 7's job and
  stays out of scope here.
- No data migration path from `categories` to `phases` -- there is no data to migrate.
- No renaming of unrelated concepts (`SessionCategory` in the roadmap's slice-7
  description becomes `SessionPhase` as a side effect of this rename existing first, but
  building that join itself is still slice 7's work, not this change's).

## Decisions

- **New D1 migration, not an edit to the old one.** `wrangler d1 migrations` files are
  immutable once applied conceptually (even though no environment has applied slice 5's
  migration outside dev/CI yet, editing history in place sets a bad precedent). The new
  migration creates `phases` and drops `categories` in one file, since there's no data to
  preserve.
- **Surrogate key prefix `CAT-` -> `PHS-`.** Every existing prefix (`PRG-` for programs,
  `SNN-` for sessions, `CAT-` for categories) is exactly 3 characters; `PHS-` keeps that
  convention rather than introducing a first 5-character prefix (`PHASE-`). Alternative
  considered: keep `CAT-` to avoid touching the ID generator's prefix constant -- rejected
  because a stale prefix on a renamed concept is more confusing than a one-line generator
  change.
- **OpenSpec capability is removed and re-added, not renamed in place.** OpenSpec's delta
  format supports `RENAMED Requirements` for a requirement name changing within the same
  capability, but not a capability folder rename. This change removes
  `specs/categories/spec.md` (via `REMOVED Requirements` covering the whole file) and adds
  `specs/phases/spec.md` (via `ADDED Requirements`, phase-flavored duplicates of the
  removed ones) so `openspec archive` produces a correct final `specs/phases/spec.md` and
  no leftover `specs/categories/`.
- **Everything renames together in one PR.** Splitting into "rename backend" then "rename
  frontend" PRs would leave an intermediate state where the Worker serves `/api/phases`
  but the Admin UI still calls `/api/categories` (or vice versa) -- there's no reason to
  ship that broken intermediate state since this is a pure rename with no new behavior to
  sequence around.

## Risks / Trade-offs

- [Doc/mockup references are easy to miss since they're prose, not compiled code] ->
  Mitigation: `tasks.md` enumerates every file found by the pre-change `Category`/
  `category`/`CAT-` grep, including `.dc.html` mockups and `canvas.json`, as an explicit
  checklist rather than relying on IDE rename-symbol (which only catches compiled
  references).
- [`CSharpier`/analyzer noise from a large mechanical rename] -> Mitigation: run
  `dotnet csharpier format` and the analyzer build as the last task, after all renames,
  rather than per-file.

## Migration Plan

1. Add the new `phases` D1 migration (create `phases`, drop `categories`); no rollback
   path needed since there's no production data.
2. Rename Worker source, tests, and routes.
3. Rename domain `CategoryId` -> `PhaseId` and its tests.
4. Rename Admin client types, page, and tests; update sidebar nav and Home tile labels.
5. Update docs and mockups.
6. Archive `specs/categories/spec.md`, add `specs/phases/spec.md`.
7. Full solution build + `dotnet csharpier format --check` + Worker `vitest` run as a
   final verification pass.

## Open Questions

None -- scope is bounded and behavior-preserving.
