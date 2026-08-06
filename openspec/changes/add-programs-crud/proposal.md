## Why

Trainfree needs a `Program` entity end to end (D1 schema, Worker API, Blazor admin UI)
before any later slice can build on it. This is the first roadmap slice and exists to
prove the Worker + D1 + Blazor + deploy pipeline works, at the smallest possible scope.

## What Changes

- Add D1 migration creating the `programs` table (`id`, `name`, timestamps).
- Add Worker routes: `GET /api/programs`, `GET /api/programs/:id`, `POST /api/programs`,
  `PATCH /api/programs/:id`, `DELETE /api/programs/:id`.
- Add a minimal Blazor admin page listing programs as rows, with `[+ Program]` to create
  a new row (inline-editable name), and `[x]` per row to delete -- per mockup 11's
  top-level rows only (no sessions/categories/exercises yet; those are later slices).
- Wire the admin page into routing/navigation.

## Capabilities

### New Capabilities
- `programs`: CRUD for the `Program` entity -- Worker API routes, D1 persistence, and the
  Blazor admin UI for listing, creating, renaming, and deleting programs.

### Modified Capabilities
(none -- first slice, no prior capabilities exist)

## Impact

- New: `src/Trainfree.Api` route handlers for `/api/programs`, D1 migration file.
- New: `src/Trainfree.Web` admin page/component for program list + inline create/delete.
- `wrangler.jsonc`: no new bindings required (D1 binding already assumed present per
  CLAUDE.md conventions; migration applies against the existing database).
