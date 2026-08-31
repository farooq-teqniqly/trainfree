## Context

The roadmap's slice 5 (`add-category-library-crud`) sits between the completed
`Programs`/`Sessions` CRUD (slices 1 and 3) and slice 7
(`add-program-categories-exercises-crud`), which will let session rows pick a category
from this library instead of typing free text. `docs/design/admin-mockups/Categories.dc.html`
draws the fully-populated end state -- a "Used in" count per row and a disabled delete
icon once a category is referenced by a session -- but that reference doesn't exist yet:
`session_categories` (the join table) isn't built until slice 7. This design covers only
what slice 5 delivers.

## Goals / Non-Goals

**Goals:**
- A flat `Category` entity (id, name) with the same CRUD shape as `Program`: list,
  create, rename, delete, all unconditional.
- A `Categories` admin page reachable from the sidebar and the `Home` tile, matching
  `CategoriesEmpty.dc.html`'s empty state and the row-level create/rename/delete pattern
  already used by `Programs.razor`.

**Non-Goals:**
- Any usage guard on delete. Nothing can reference a category yet, so there is nothing
  to check.
- The "Used in" column, the disabled-delete icon state, and the count pill's live number
  from `Categories.dc.html`'s populated screen. These require `session_categories` and
  land in slice 7.
- The category picker on session rows (slice 7).

## Decisions

### Category is flat, not nested

`Program` and `Session` established two shapes: a top-level resource
(`/api/programs`) and a resource nested under its parent
(`/api/programs/:programId/sessions`). `Category` is top-level like `Program` --
`/api/categories` -- since nothing owns a category; sessions will *reference* one
(slice 7), not contain one. `categories.js` mirrors `programs.js` function-for-function
(`listCategories`, `createCategory`, `renameCategory`, `deleteCategory`), not
`sessions.js`'s parent-scoped variants.

### Delete guard deferred to slice 7, not built as inert scaffolding now

Considered building the FK check now (always allowing delete since no rows can ever
reference a category) so slice 7 only has to add the join table. Rejected: it would be
dead code with no test that can fail meaningfully today, and CLAUDE.md's baseline rule
against designing for hypothetical future requirements applies directly. `deleteCategory`
is a plain unconditional `DELETE`, identical to `deleteProgram`, until slice 7 gives it
something to check against.

### Slice 7's delete guard will be reactive (409 on conflict), not precomputed

Decided ahead of slice 7, to avoid slice 5 shipping a response shape that would need to
change: `GET /api/categories` returns `{ id, name, createdAt, updatedAt }` only, with no
`usedCount` field, permanently. When slice 7 adds the guard, `deleteCategory` will
attempt the delete and translate a foreign-key violation from `session_categories` into
a typed error (same pattern as `DuplicateNameError` in `errors.js`), which the Worker
maps to `409` with a message like `"Category is used by 3 sessions"`. The alternative --
a `LEFT JOIN session_categories ... GROUP BY` on every `GET /api/categories` call to
precompute a count for the mockup's disabled-icon affordance -- was rejected as a
permanent list-query cost paid on every load to serve a pre-emptive UI state, when the
codebase already has a working try-then-typed-failure pattern (`DeleteProgramFailed`,
`RenameProgramFailed` in `Programs.razor`) that a reactive 409 fits directly. This
decision doesn't change anything slice 5 builds -- it's recorded here so slice 7 doesn't
re-litigate it and so a future reader comparing `Categories.dc.html`'s disabled icon to
the shipped UI understands the gap is deliberate.

### Mockup deviation is intentional, not a drift to reconcile

`Categories.dc.html` is the destination screen for *after* slice 7, not a spec slice 5
must fully match. Slice 5 ships `CategoriesEmpty.dc.html`'s state (and the populated
list's name/actions column only) permanently until slice 7 adds the "Used in" column.
The mockup files themselves are not edited by this change -- see `docs/design/admin-mockups/README.md`.

## Risks / Trade-offs

- [A category deleted here could later need slice 7's guard retrofitted onto rows a user
  already deleted] -> Not a real risk: nothing references categories until slice 7 ships,
  so no session can be left dangling by an earlier unconditional delete.
- [Two admin pages (`Programs`, `Categories`) now duplicate the same working/saved-value
  dirty-row Blazor pattern with no shared component] -> Accepted for this slice, same as
  the existing `Programs`/`Sessions` duplication within one page; a shared row component
  is not introduced speculatively here (CLAUDE.md: no abstraction beyond what's needed).

## Migration Plan

Standard slice migration: `wrangler d1 migrations` file adds `categories`, applied by
`deploy.yaml` on the next tag. No backfill -- the table starts empty. No rollback
concern beyond the usual migration-revert path; no other table references `categories`
yet.
