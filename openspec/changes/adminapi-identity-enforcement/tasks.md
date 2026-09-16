## 1. Wire the IdentityApi service binding into AdminApi's config

- [x] 1.1 Add a `services` binding (`IDENTITY` -> `trainfree-identity-api`) to
  `src/Trainfree.AdminApi/wrangler.jsonc` and `wrangler.deploy.jsonc`, and add
  `LOCAL_DEV_BYPASS: "true"` under `wrangler.jsonc`'s `vars` only (never in
  `wrangler.deploy.jsonc`). Verify by running `wrangler dev` in `Trainfree.AdminApi`
  with `Trainfree.IdentityApi`'s own `wrangler dev` running alongside it and confirming
  both start with no config errors.
- [x] 1.2 Add `ADMIN_INTERNAL_KEY` to `Trainfree.AdminApi/.dev.vars.example` (mirroring
  `Trainfree.IdentityApi/.dev.vars.example`'s existing entry) and document in
  `Trainfree.AdminApi`'s `.dev.vars` setup that the same value must be copied into both
  Workers' `.dev.vars`. Verify by diffing the two `.dev.vars.example` files' key names.
- [x] 1.3 Document in README.md's "Cloudflare Access application" / rollout section
  that production requires `wrangler secret put ADMIN_INTERNAL_KEY` against **both**
  `trainfree-admin` and `trainfree-identity-api` with the same value (a manual,
  one-time step, same category as the existing Access application setup) -- this is
  not automated in `deploy.yaml`. Verify by re-reading the added paragraph for
  correctness against `CLAUDE.md`'s "Prod API URL is never configured" rule section.

## 2. Build the enforcement seam

- [x] 2.1 Add `src/Trainfree.AdminApi/src/identity.js` exporting a function that calls
  `env.IDENTITY.fetch` with `X-Trainfree-Caller: admin` and the request's
  `CF_Authorization` cookie forwarded, and normalizes the result to one of:
  `{ ok: true, identity }` (IdentityApi's `200` body), `{ ok: false, status: 401 }`,
  `{ ok: false, status: 403 }`, or `{ ok: false, status: 503 }` (covers IdentityApi's
  own `503`, any other unexpected status including `404`, and a thrown/timed-out
  fetch). Verify with unit tests in `identity.test.js` covering each of the four
  outcomes plus the thrown-fetch case, using a fake `env.IDENTITY.fetch`.
- [x] 2.2 In the same module, add the `LOCAL_DEV_BYPASS` branch: when
  `env.LOCAL_DEV_BYPASS` is set, skip the service-binding call and instead look up the
  local seed identity's `userId` from `env.DB` (querying `logins`/`users` for
  `provider_name = "cloudflare-access"`, `provider_id = "local-dev@trainfree.local"`),
  returning the synthetic
  `{ email: "local-dev@trainfree.local", role: "Administrator", userId }` shape as
  `{ ok: true, identity }`. Verify with a test using a real Miniflare D1 binding
  seeded with that row, asserting the returned `userId` matches the seeded row, and a
  second test asserting a missing seed row surfaces a clear error rather than a
  silent bad `userId`.
- [x] 2.3 Add a role-check helper (`isAdministrator(identity)`) used by every enforced
  endpoint except `/api/me`. Verify with unit tests for `Administrator` and `User`
  inputs.

## 3. Enforce every data endpoint in index.js

- [x] 3.1 Wrap every existing route handler call in `index.js`'s `fetch` (programs,
  programs-tree, phases, exercises, sessions, session-phases, program-exercises --
  collections and resources) with a call to the module from task 2: on `ok: false`,
  return that status verbatim (401/403/503) without calling the handler; on
  `ok: true` with a non-Administrator identity, return `403` without calling the
  handler; otherwise proceed to the existing handler unchanged. Verify with
  `index.test.js` cases for `GET /api/programs-tree` and one nested route (e.g.
  `GET /api/programs/:id/sessions`) each returning 401/403/503 from the enforcement
  call before their handler runs, and returning their normal 200/404/etc. body when
  the identity is Administrator.
- [x] 3.2 Leave `GET /api/version` and `OPTIONS` handling exactly as-is (no
  enforcement call). Verify with a test asserting `GET /api/version` and an `OPTIONS`
  request make no call into the module from task 2 (e.g. via a spy `env.IDENTITY`
  that fails the test if `fetch` is invoked).
- [x] 3.3 Add the `GET /api/me` route: calls the enforcement module, and on
  `ok: true` returns `200 { email, role }` (identity minus `userId`) for either role;
  on `ok: false` relays that status verbatim. Verify with tests for an Administrator
  identity, a User identity, and each of the 401/403/503 outcomes.

## 4. Attribute created programs to the caller

- [x] 4.1 Change `createProgram(db, name)` in `programs.js` to
  `createProgram(db, name, userId)` and set `user_id` on the inserted row. Update the
  `POST /api/programs` handler to pass the enforcement call's resolved `userId`.
  Verify with a `programs.test.js` case asserting the created row's `user_id` equals
  the passed value.
- [x] 4.2 Re-run existing `programs.test.js`/`index.test.js` create-program cases and
  update any call sites broken by the new parameter. Verify with `npm test` passing
  in `Trainfree.AdminApi`.

## 5. Local dev seed step

- [x] 5.1 Confirm `Trainfree.IdentityApi/scripts/provision-identity.js` (already
  idempotent per the `identity-api` spec) can seed the fixed local identity via
  `npm run provision -- --email local-dev@trainfree.local --role Administrator` run
  from `Trainfree.IdentityApi` against local D1 (its default, non-`--remote` target).
  Verify by running it against a freshly reset local D1
  (`rm -rf src/.wrangler-shared` then migrate) and confirming a `logins`/`users` row
  is created, then re-running and confirming it reports already-provisioned with no
  new writes.
- [x] 5.2 Update README.md's "Local development" section: add this seed command as a
  required one-time step before `LOCAL_DEV_BYPASS` endpoints will resolve a real
  `userId`, and note that `LOCAL_DEV_BYPASS` is now set by default in
  `Trainfree.AdminApi/wrangler.jsonc`. Verify by re-reading the updated section
  end-to-end against the actual steps a fresh clone would need.

## 6. End-to-end verification

- [x] 6.1 Add an `index.test.js` case asserting that with `LOCAL_DEV_BYPASS` set and
  the seed row present, `POST /api/programs` succeeds and the created row's `user_id`
  matches the seeded local identity's `userId`, exercising the same handler code path
  a real Administrator would. Verify it passes.
- [x] 6.2 Add an `index.test.js` case asserting that with `LOCAL_DEV_BYPASS` unset and
  `env.IDENTITY` absent (simulating a deployed environment missing the binding), any
  enforced endpoint responds `503`. Verify it passes.
- [x] 6.3 Run the full `Trainfree.AdminApi` suite (`npm test`) and confirm all
  existing tests (programs, phases, exercises, sessions, session-phases,
  program-exercises, programs-tree, version, migrations) still pass unmodified aside
  from the `createProgram` signature change in task 4.
