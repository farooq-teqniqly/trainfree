## 1. D1 Schema

- [x] 1.1 Add a `wrangler d1 migrations` migration file creating the `programs` table
      (`id INTEGER PRIMARY KEY AUTOINCREMENT`, `program_id TEXT NOT NULL UNIQUE`,
      `name TEXT NOT NULL`, `created_at TEXT NOT NULL`, `updated_at TEXT NOT NULL`).
- [x] 1.2 Apply the migration locally and confirm the table exists via `wrangler d1
      execute` against the local dev database.

## 2. Worker API

- [x] 2.1 Write failing `vitest` tests for an `ids.js` helper: generates `PRG-XXXXXX`
      (6 Crockford base32 chars), and validates that shape (accept/reject cases).
- [x] 2.2 Implement `ids.js` (generate + validate) to pass.
- [x] 2.3 Write a failing `vitest` test for `GET /api/programs` (empty list) against a
      real D1 binding via Miniflare.
- [x] 2.4 Implement `GET /api/programs` (list by `created_at`, expose `program_id` as
      `id`, never the internal autoincrement key) to pass the test.
- [x] 2.5 Write failing tests for a `name` length validator (5-100 chars after trim):
      accepts boundary values (exactly 5, exactly 100); rejects too-short (including
      blank), too-long, and missing.
- [x] 2.6 Implement the length validator to pass.
- [x] 2.7 Write failing tests for `POST /api/programs`: valid name creates a row with a
      generated `program_id` and returns `201`; name failing the length bound returns
      `400` with no row created.
- [x] 2.8 Implement `POST /api/programs` (generated surrogate ID via `ids.js`,
      timestamps, length validation) to pass.
- [x] 2.9 Write failing tests for `PATCH /api/programs/:id`: valid rename updates and
      returns `200`; unknown ID returns `404`; name failing the length bound returns
      `400`.
- [x] 2.10 Implement `PATCH /api/programs/:id` to pass.
- [x] 2.11 Write failing tests for `DELETE /api/programs/:id`: existing ID deletes and
      returns `204`; unknown ID returns `404`.
- [x] 2.12 Implement `DELETE /api/programs/:id` to pass.
- [x] 2.13 Write a failing test confirming `GET /api/programs` returns multiple programs
      in creation order, then confirm it passes against the implementation above.

## 3. Blazor Admin UI

- [x] 3.1 Write failing xUnit tests for a `ProgramId` readonly record struct
      (`Trainfree.Web`, mirrors trakmark's `CityId` shape but consumer-only, no
      generation): `Parse`/`TryParse` accept a well-formed `PRG-XXXXXX` value and
      reject ill-formed input; `ToString` round-trips the original value.
- [x] 3.2 Implement `ProgramId` and a shared `CrockfordBase32.IsValidBody` validation
      helper (reusable by later slices' ID types) to pass. No `NewId()` -- IDs are
      always Worker-assigned and arrive via the API.
- [x] 3.3 Write a failing bUnit test: admin page loads and renders one row per program
      returned by a stubbed API client.
- [x] 3.4 Implement the admin page's initial load (calls `GET /api/programs`, renders
      rows) to pass.
- [x] 3.5 Write a failing bUnit test: clicking `[+ Program]` calls create, appends a row,
      and puts its name cell into edit mode.
- [x] 3.6 Implement `[+ Program]` create flow to pass.
- [x] 3.7 Write a failing bUnit test: editing a row's name and blurring calls `PATCH`
      and updates the displayed name.
- [x] 3.8 Implement inline rename-on-blur to pass.
- [x] 3.9 Write a failing bUnit test: clicking `[x]` calls `DELETE` and removes the row.
- [x] 3.10 Implement `[x]` delete flow to pass.

## 4. Wiring and Verification

- [ ] 4.1 Register the admin page route/navigation entry in the Blazor app.
- [ ] 4.2 Confirm `appsettings.Development.json`'s API base address
      (`http://localhost:8787/api`) resolves against `wrangler dev` for local
      end-to-end testing.
- [ ] 4.3 Manually verify the full flow locally: add, rename, delete a program through
      the running Blazor app against the local Worker + D1.
- [ ] 4.4 Run full test suites (`vitest` for the Worker, `dotnet test` for Blazor) and
      confirm all pass.
