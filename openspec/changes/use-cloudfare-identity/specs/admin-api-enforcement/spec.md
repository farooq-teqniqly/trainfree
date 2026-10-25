## ADDED Requirements

### Requirement: Identity requests name the application, authenticate it, and carry the user's token
`AdminApi` SHALL resolve every caller's identity by calling cloudfare-identity's
`GET /v1/identity` over a service binding, sending `X-Identity-Application:
Trainfree.Admin`, `X-Identity-Credential` set to the value of the existing
`ADMIN_INTERNAL_KEY` secret (no rotation), and the end user's Cloudflare Access token in
`X-Identity-Token`. The token SHALL be taken from the Access-supplied token on the
request, not from the `CF_Authorization` cookie, which Access rewrites on requests to a
gated Worker. `AdminApi` SHALL NOT verify the token itself; verification, including the
audience check for `Trainfree.Admin`, belongs to cloudfare-identity.
**Rationale**: One shared identity service verifies tokens for every app, so Trainfree no
longer duplicates JWT/JWKS logic. Reusing the existing secret value avoids a coordinated
rotation across two deployed Workers.

#### Scenario: Request carries application, credential and token headers
- **WHEN** `AdminApi` resolves the identity for an authenticated request
- **THEN** the call to cloudfare-identity carries `X-Identity-Application:
  Trainfree.Admin`, `X-Identity-Credential` equal to `ADMIN_INTERNAL_KEY`, and
  `X-Identity-Token` equal to the request's Access token

#### Scenario: Token comes from the Access-supplied token, not the rewritten cookie
- **WHEN** a request carries both a rewritten `CF_Authorization` cookie and the
  Access-supplied token
- **THEN** `X-Identity-Token` holds the Access-supplied token

#### Scenario: Resolved shape is unchanged for callers
- **WHEN** cloudfare-identity returns `200 { "email": "a@x.com", "userId": "USR-S6PHKJ",
  "role": "Administrator" }`
- **THEN** `AdminApi` treats the identity as resolved with that `email`, `userId` and
  `role`, exactly as it did for the previous identity source

### Requirement: Program data moves to a new database without identity tables
After the switch to cloudfare-identity is verified in production, `AdminApi` SHALL be
cut over to a new D1 database that holds the app tables only: no `logins`, `users` or
`roles` tables, and no foreign key from `programs.user_id` to `users`. Every `programs`
row and every row beneath it (sessions, session phases, program exercises), and every
other app table's rows, SHALL be copied with unchanged values, including each
`programs.user_id`. The new database's `programs` SHALL still reject a NULL `user_id` on
insert and on update of that column. The original database SHALL remain intact and
unmodified until the new one is verified in production, and SHALL be deleted only after
that.
**Rationale**: Removing the foreign key in place would mean rebuilding `programs`, which
risks cascading deletes through every child table; copying to a database created with the
right schema avoids touching live data at all, and keeps a rollback path (repoint the
binding) until the new database is proven. `programs.user_id` values stay valid because
the user's `userId` was preserved when they were loaded into cloudfare-identity.

#### Scenario: Program data is copied intact
- **WHEN** the data is exported from the original database and imported into the new one
- **THEN** every app table has the same row count in both databases, every program and
  descendant row has unchanged values, and the program owned by `USR-S6PHKJ` still has
  that `user_id`

#### Scenario: New database has no identity tables
- **WHEN** the new database's schema is inspected
- **THEN** `logins`, `users` and `roles` do not exist and `programs.user_id` has no
  foreign key

#### Scenario: Writes to programs still work after the cutover
- **WHEN** an Administrator creates a program after `AdminApi` is cut over
- **THEN** the insert succeeds with the caller's `userId` as owner, and an insert or
  update setting a NULL `user_id` is still rejected

#### Scenario: Original database is untouched before verification
- **WHEN** the cutover has been deployed but the production check against the new
  database has not yet passed
- **THEN** the original database still exists with all its rows unchanged

### Requirement: IdentityApi and its provisioning are retired
`Trainfree.IdentityApi` SHALL no longer exist as a source project, a service binding, a
deploy and version-polling step in CI, a CI test target, a local-dev orchestration entry,
or a provisioning script, and no remaining project, workflow or document SHALL depend
on it.
**Rationale**: Leaving a second, unused identity implementation deployable duplicates the
logic this change moves out and invites drift between the two.

#### Scenario: No deploy or CI reference remains
- **WHEN** `deploy.yaml`, `ci.yaml` and the local-dev orchestration are inspected
- **THEN** none of them builds, tests, deploys, polls or starts `Trainfree.IdentityApi`

## RENAMED Requirements

- FROM: `### Requirement: Every data endpoint calls IdentityApi before executing`
- TO: `### Requirement: Every data endpoint resolves identity before executing`

- FROM: `### Requirement: IdentityApi's own 401/403 are relayed verbatim`
- TO: `### Requirement: The identity service's own 401/403 are relayed verbatim`

- FROM: `### Requirement: IdentityApi outages surface as 503, never folded into 403`
- TO: `### Requirement: Identity service outages surface as 503, never folded into 403`

## MODIFIED Requirements

### Requirement: Every data endpoint resolves identity before executing
Every `AdminApi` route that reads or writes program data SHALL resolve the caller's
identity through cloudfare-identity's `GET /v1/identity` over the service binding,
sending `X-Identity-Application: Trainfree.Admin`, before running its own handler logic.
This applies to every CRUD route and every non-CRUD read, explicitly including
`GET /api/programs-tree` and the nested collection/resource GETs under
`/api/programs/:id/sessions/...`.
**Rationale**: A route that skips this call and falls through to its handler is
reachable by any authenticated, provisioned identity regardless of role; naming the
non-CRUD reads explicitly closes the gap an audit of only the obviously-mutating routes
would miss.

#### Scenario: Programs-tree read is enforced
- **WHEN** a request reaches `GET /api/programs-tree`
- **THEN** `AdminApi` resolves the identity and evaluates the result before calling
  `listProgramsTree`

#### Scenario: Nested session/phase/exercise reads are enforced
- **WHEN** a request reaches any nested GET under `/api/programs/:id/sessions/...`
  (sessions, session-phases, or program-exercises collections or resources)
- **THEN** `AdminApi` resolves the identity and evaluates the result before running that
  route's handler

### Requirement: Non-Administrator identities are rejected with 403
`AdminApi` SHALL, for every enforced endpoint except `GET /api/me` and
`GET /api/exercises/:id/image`, inspect the body of a `200` identity response and
respond `403` itself, without calling the underlying handler, when
`role !== "Administrator"`.
**Rationale**: The identity service's `200` means "has a role in this application," not
"Administrator" -- it returns `200` for a `User` identity too. Relaying only its own
`401`/`403` verbatim would let a `User` through to every Administrator-only endpoint,
since the identity service never itself returns `403` for "resolved, but wrong role."

#### Scenario: Provisioned User is denied a data endpoint
- **WHEN** the identity service returns `200` with `role: "User"` for a request to
  `POST /api/programs`
- **THEN** `AdminApi` responds `403` and does not call `createProgram`

#### Scenario: Provisioned Administrator is allowed through
- **WHEN** the identity service returns `200` with `role: "Administrator"` for a request
  to any enforced endpoint
- **THEN** `AdminApi` runs that endpoint's normal handler and returns its result

### Requirement: The identity service's own 401/403 are relayed verbatim
`AdminApi` SHALL relay the identity service's own `401` (could not authenticate the
request at all) or `403` (authenticated but the login has no role in `Trainfree.Admin`)
verbatim to the caller and SHALL NOT run the underlying handler.
**Rationale**: These are the identity service's own denial outcomes, distinct from the
role-mismatch `403` `AdminApi` synthesizes itself; passing them through unchanged keeps
one consistent meaning for each status code reaching the browser.

#### Scenario: Unauthenticated request is rejected
- **WHEN** the identity service responds `401` for a request to any enforced endpoint
- **THEN** `AdminApi` responds `401` and does not run the underlying handler

#### Scenario: Unprovisioned identity is rejected
- **WHEN** the identity service responds `403` because the login has no role in
  `Trainfree.Admin`
- **THEN** `AdminApi` responds `403` and does not run the underlying handler

### Requirement: A malformed optional sessionId is dropped, not fatal
When the identity service returns a `200` whose optional `sessionId` is present but is
not 32 lowercase hex characters, `AdminApi` SHALL discard it and treat the identity as
resolved without a `sessionId`, rather than responding `503`. Validation of `email`,
`userId` and `role` is unchanged: a missing or malformed value there still yields `503`.
**Rationale**: `sessionId` is optional observability metadata; losing it must not become
an outage.

#### Scenario: Malformed sessionId does not fail the request
- **WHEN** the identity service returns `200` with a valid `email`, `userId` and `role`
  and a `sessionId` of `"abc"`
- **THEN** `AdminApi` proceeds as for any resolved identity, with no `session.id` set

### Requirement: GET /api/me returns any provisioned identity's role
`AdminApi` SHALL expose `GET /api/me`, which resolves identity the same way every other
endpoint does but SHALL NOT apply the Administrator-only check: it returns
`200 { "email": string, "role": "Administrator" | "User" }` for any identity the identity
service resolves, relaying the `200` body minus the internal `userId` and `sessionId`
fields. It relays the identity service's `401`/`403`/`503` the same as every other
endpoint.
**Rationale**: This is the one endpoint a `User`-role caller must be able to reach
successfully, so the client (slice 8c) can distinguish "denied by role" from "not
logged in" -- omitting `userId` and `sessionId` keeps internal identifiers out of a
response the browser has no use for.

#### Scenario: Administrator identity via /api/me
- **WHEN** the identity service returns `200 { "email": "a@x.com", "userId": "1", "role":
  "Administrator", "sessionId": "<32 hex>" }` for a `GET /api/me` request
- **THEN** `AdminApi` responds `200 { "email": "a@x.com", "role": "Administrator" }`

#### Scenario: User identity via /api/me succeeds, unlike other endpoints
- **WHEN** the identity service returns `200` with `role: "User"` for a `GET /api/me`
  request
- **THEN** `AdminApi` responds `200` with that role, not `403`

### Requirement: GET /api/exercises/:id/image is readable by any provisioned identity
`AdminApi` SHALL allow `GET /api/exercises/:id/image` for any provisioned identity,
Administrator or User, skipping the Administrator-only check for this route alone as
`GET /api/me` does. It SHALL still resolve identity first, relay the identity service's
own `401` and `403` verbatim without reading R2, and apply the same `503` handling as
every other endpoint. `PUT` and `DELETE` on `/api/exercises/:id/image` remain
Administrator-only.
**Rationale**: The workout app is used by the `User` role and shows these images, so
blocking `User` from reading them would break that app; reading an image exposes nothing
a `User` cannot already see in the workout, while changing or removing one stays an admin
action. This is the second deliberate exception to the Administrator-only rule, alongside
`GET /api/me`.

#### Scenario: User reads an exercise image
- **WHEN** the identity service returns `200` with `role: "User"` for a
  `GET /api/exercises/:id/image` request
- **THEN** `AdminApi` responds `200` with the image bytes, not `403`

#### Scenario: User cannot change an exercise image
- **WHEN** the identity service returns `200` with `role: "User"` for a `PUT` or `DELETE
  /api/exercises/:id/image` request
- **THEN** `AdminApi` responds `403` and changes nothing

### Requirement: Created programs are owned by the caller
Every write endpoint that creates a `programs` row (`createProgram`, and any future
owner-scoped create) SHALL set `programs.user_id` to the `userId` field from the same
identity `200` response already inspected for the role check.
**Rationale**: The response already carries `userId`; a separate D1 lookup to populate
the owner column would duplicate a value already in hand from the enforcement call.

#### Scenario: New program is owned by the authenticated caller
- **WHEN** an Administrator identity with `userId: "USR-S6PHKJ"` calls `POST /api/programs`
- **THEN** the created `programs` row has `user_id` equal to `"USR-S6PHKJ"`

### Requirement: Identity service outages surface as 503, never folded into 403
`AdminApi` SHALL surface a `503` from the identity service, or a service-binding call
that itself throws or times out, as `503`. Any identity service response status other
than `200`/`401`/`403`/`503` -- including the `404` it returns for an unidentified caller
(unregistered application or wrong credential) -- SHALL also be treated as `503`. In
every one of these cases, and in the `401`/`403` cases above, `AdminApi` SHALL fail
closed: it never runs the underlying operation unless the identity service positively
returned `200`.
**Rationale**: An infrastructure outage must stay distinguishable from a denied role so
the UI can show "something went wrong, retry" rather than "no access"; a `404` here means
a misconfigured application name or credential, not a real "not found," so treating it
as success or as a routing 404 would either bypass enforcement or mislead the caller
about the actual failure.

#### Scenario: IdentityApi 503 is relayed as 503
- **WHEN** the identity service responds `503` to the service-binding call
- **THEN** `AdminApi` responds `503` and does not run the underlying handler

#### Scenario: Unexpected IdentityApi status is treated as 503
- **WHEN** the identity service responds `404` (unregistered application or wrong
  credential) to the service-binding call
- **THEN** `AdminApi` responds `503`, not `404` and not success

#### Scenario: Service-binding call that throws is treated as 503
- **WHEN** the service-binding call to the identity service throws or times out instead
  of returning a response
- **THEN** `AdminApi` responds `503` and does not run the underlying handler

### Requirement: Version and OPTIONS are exempt from enforcement
`GET /api/version` and `OPTIONS` preflight requests SHALL bypass identity resolution
entirely, for every caller.
**Rationale**: `GET /api/version` is polled by `deploy.yaml`'s CI verification step
using a service-token credential with no user token to check, and by the client's
same-origin version check; routing either through the per-user token check would make the
CI verification step fail every deploy. `OPTIONS` carries no token at all.

#### Scenario: Version endpoint is reachable without a JWT
- **WHEN** a request with no Access token reaches `GET /api/version`
- **THEN** `AdminApi` responds normally with the version stamp, making no call to the
  identity service

#### Scenario: OPTIONS preflight is not enforced
- **WHEN** an `OPTIONS` request reaches any route
- **THEN** `AdminApi` responds with CORS headers and makes no call to the identity
  service

### Requirement: Local dev bypasses the service-binding call, not the role check
`AdminApi` SHALL support a `LOCAL_DEV_BYPASS` environment flag, set only in local
`wrangler dev` configuration and never in any deployed configuration. When set,
`AdminApi` SHALL substitute a fixed synthetic local identity
(`{ "email": "local-dev@trainfree.local", "role": "Administrator", "userId": <a fixed
synthetic id> }`) for the identity service's result, with no database lookup and no
setup script, then continue running each endpoint's normal handler and
Administrator-role check against that synthetic identity exactly as it would for a real
`200` -- including `/api/me`, which relays this identity to the caller. A deployed
environment where the `services` binding or the credential secret is absent SHALL fail
closed with `503`; it SHALL NOT fall back to bypass behavior. The synthetic identity
SHALL NOT be seeded by any committed migration or reachable via any `--remote` path.
**Rationale**: There is no Cloudflare Access edge in front of `wrangler dev`, so no token
ever reaches `AdminApi` locally; substituting the identity rather than skipping the
endpoint's own checks keeps local dev exercising the same authorization code path
production runs. With the identity tables gone there is no local row for the id to
reference, so a fixed value replaces the seeded lookup. Detecting "local" by an explicit
flag rather than "binding/credential missing" keeps a production deploy that is
accidentally missing either failing closed instead of silently behaving like local dev.

#### Scenario: Local bypass substitutes identity, not the handler
- **WHEN** `LOCAL_DEV_BYPASS` is set and a request reaches `POST /api/programs`
- **THEN** `AdminApi` makes no call to the identity service, runs `createProgram` using
  the synthetic identity's `userId` as the row's owner, and does not skip the
  Administrator-role check

#### Scenario: Deployed environment missing the binding fails closed
- **WHEN** `LOCAL_DEV_BYPASS` is not set and the identity service binding or credential
  secret is absent
- **THEN** `AdminApi` responds `503` to enforced endpoints rather than substituting the
  local identity

#### Scenario: Seed script never runs against the remote database
- **WHEN** a local-dev setup is run
- **THEN** no identity seed script exists or runs, and nothing identity-related is written
  to the local or remote database

#### Scenario: Local bypass needs no database seed
- **WHEN** `LOCAL_DEV_BYPASS` is set against a freshly migrated local database with no
  identity rows or tables
- **THEN** a request resolves to the synthetic Administrator identity without error

## Decisions

- **Switch to cloudfare-identity and delete the internal Worker, over keeping both**
  (issue #158): Chose a single shared identity service. Keeping `Trainfree.IdentityApi`
  alongside it duplicates JWT/JWKS and role logic and couples identity fixes to Trainfree
  releases. Keeping both would be right only if cloudfare-identity could not serve
  `Trainfree.Workout`'s needs once that app exists; it is out of scope until its Access
  application and audience exist.

- **Reuse `ADMIN_INTERNAL_KEY` as the credential, no rotation**: cloudfare-identity names
  its per-application secret `ADMIN_INTERNAL_KEY` for `Trainfree.Admin`, so the existing
  value is set there unchanged. Rotating would be right only if the old value had been
  exposed.

- **Token from the Access-supplied token, not the cookie**: Access rewrites
  `CF_Authorization` on requests to a gated Worker, so the cookie no longer carries a
  token cloudfare-identity can verify. The cookie fallback cloudfare-identity offers is
  for local development only and is not used here.

- **Two-stage delivery**: Stage one ships the switch to cloudfare-identity and the code
  removals, against the existing database. After the production check passes, stage two
  cuts `AdminApi` over to the new database. A single stage would be right only if a
  failed switch could be reverted independently of the database change, which it cannot
  once the binding points at a new database.

- **Copy to a new database rather than drop tables in place**: `programs.user_id`
  currently references `users(user_id)`, and D1 enforces foreign keys, so once `users` is
  gone any insert into `programs` would fail unless the reference is removed. SQLite
  cannot drop a constraint, so removing it means rebuilding `programs`, and migration 0016
  documents that dropping `programs` while child tables cascade from it deletes the whole
  tree. Chose a new database created with the correct schema plus a data copy, over an
  in-place rebuild, because the original stays untouched and rollback is a binding change.
  An in-place rebuild would be right only if D1 offered a documented safe way to drop a
  foreign key without cascading.

- **Single baseline migration for the new database**: The new database starts from one
  baseline migration replacing `0001`-`0017` in the migrations folder, with the old
  history preserved in git. Keeping the historical migrations and dropping the identity
  tables with a final one would hit the same foreign key problem. The old files would be
  worth keeping only if the original database were to keep being migrated, which it is
  not.

- **Fixed synthetic id for local dev**: the seeded lookup read `logins`/`users`, which no
  longer exist, so the bypass returns a constant. A seeded local row would be right only
  if a foreign key on `programs.user_id` were retained.

- **Writes are paused during the cutover rather than replicated**: The app has one user,
  so avoiding use between export and repointing the binding is simpler than any sync
  step. Replication would be right only if the app gained concurrent users.

## Requirement coverage

Anchor: GitHub issue #158 (feat(identity): use cloudfare-identity and retire the internal IdentityApi Worker)

| # | Anchor requirement | Covered by |
|---|--------------------|-----------|
| 1 | Bind cloudfare-identity as the identity service in `wrangler.jsonc` / `wrangler.deploy.jsonc` | Req: Identity requests name the application... (service binding); tasks.md |
| 2 | `identity.js` calls `GET /v1/identity` with application, credential (`ADMIN_INTERNAL_KEY`, no rotation) and user token headers | Req: Identity requests name the application, authenticate it, and carry the user's token |
| 3 | Map `401`, `403`, `404`, `503` to today's outcomes, keep `{ email, userId, role }` | Reqs: The identity service's own 401/403 are relayed verbatim; Identity service outages surface as 503; Identity requests... (Scenario: Resolved shape is unchanged) |
| 4 | vitest: each status and the success shape | tasks.md test tasks, one per scenario above |
| 5 | Remove `src/Trainfree.IdentityApi`, service binding, CI stamp polling, provisioning script | Req: IdentityApi and its provisioning are retired; capability `identity-api` REMOVED |
| 6 | Migration dropping identity tables from the shared D1 database | Req: Program data moves to a new database without identity tables; see Decisions (copy to new database replaces an in-place drop, baseline migration) |
| 7 | Delete `openspec/specs/identity-api`; update `docs/identity/*` and rollout runbook | Capability `identity-api` REMOVED; docs updates in tasks.md |
| 8 | Verify in prod: admin login resolves `USR-S6PHKJ` / `Administrator` | Not a spec requirement - production verification task in tasks.md |
| 9 | Verify no-access path returns `403` | Req: The identity service's own 401/403 are relayed verbatim (Scenario: Login with no role...); production check in tasks.md |
| 10 | Note: Trainfree no longer verifies JWTs | Req: Identity requests... (AdminApi SHALL NOT verify the token) |
| 11 | Note: user's token goes in `X-Identity-Token`, not the rewritten cookie | Req: Identity requests... (Scenario: Token comes from the Access-supplied token) |
| 12 | Note: take a D1 backup first; drop only after prod check passes | Req: Program data moves to a new database... (Scenario: Original database is untouched before verification); the original database is the backup, deleted only after verification |
| 13 | Note: Workout not registered; moving it is out of scope | Not covered - out of scope until its Access application and audience exist; see Decisions |
| 14 | AC: login resolution goes to cloudfare-identity with same userId, email, role | Req: Identity requests... (Scenario: Resolved shape is unchanged) |
| 15 | AC: login with no role in `Trainfree.Admin` refused as today | Req: The identity service's own 401/403 are relayed verbatim |
| 16 | AC: IdentityApi, binding, tables, spec no longer exist, nothing depends on them | Reqs: IdentityApi and its provisioning are retired; Program data moves to a new database without identity tables; capability `identity-api` REMOVED |
| 17 | AC: prod check recorded in the PR | Not a spec requirement - process step in tasks.md |
