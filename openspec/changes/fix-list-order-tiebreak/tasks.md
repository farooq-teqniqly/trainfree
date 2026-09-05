## 1. Fix listPrograms tiebreak

- [x] 1.1 In `src/Trainfree.AdminApi/src/programs.js`, change `listPrograms`'s query to
      `ORDER BY created_at ASC, programs.id ASC` and verify the query still parses/runs
      via the existing `index.test.js` program-listing tests
- [x] 1.2 In `src/Trainfree.AdminApi/src/index.test.js`, add a test mirroring the
      sessions tiebreak test at line 290 (`breaks a created_at tie using insertion
      order`): insert two programs with an identical `created_at`, call
      `GET /api/programs`, and assert deterministic order by `id`

## 2. Fix listPhases tiebreak

- [x] 2.1 In `src/Trainfree.AdminApi/src/phases.js`, change `listPhases`'s query to
      `ORDER BY created_at ASC, phases.id ASC` and verify the query still parses/runs
      via the existing `index.test.js` phase-listing tests
- [x] 2.2 In `src/Trainfree.AdminApi/src/index.test.js`, add a test mirroring the
      sessions tiebreak test at line 290: insert two phases with an identical
      `created_at`, call `GET /api/phases`, and assert deterministic order by `id`

## 3. Verify

- [x] 3.1 Run `npm test` (vitest) in `src/Trainfree.AdminApi` and verify the full suite
      passes, including the two new tiebreak tests
