# admin-api-enforcement Specification

## Purpose

Enforces the Administrator role on every `Trainfree.AdminApi` data endpoint by calling
`Trainfree.IdentityApi` over its service binding before carrying out the underlying
operation, closing the security gap left open by slice 8a (`IdentityApi` deployed but
uncalled).

## Requirements

### Requirement: Every data endpoint calls IdentityApi before executing
Every `AdminApi` route that reads or writes program data SHALL call `IdentityApi`'s
`GET /internal/identity` over the service binding, sending `X-Trainfree-Caller: admin`,
before running its own handler logic. This applies to every CRUD route and every
non-CRUD read, explicitly including `GET /api/programs-tree` and the nested
collection/resource GETs under `/api/programs/:id/sessions/...`.
**Rationale**: A route that skips this call and falls through to its handler is
reachable by any authenticated, provisioned identity regardless of role; naming the
non-CRUD reads explicitly closes the gap an audit of only the obviously-mutating routes
would miss.

#### Scenario: Programs-tree read is enforced
- **WHEN** a request reaches `GET /api/programs-tree`
- **THEN** `AdminApi` calls `IdentityApi` and evaluates the result before calling
  `listProgramsTree`

#### Scenario: Nested session/phase/exercise reads are enforced
- **WHEN** a request reaches any nested GET under `/api/programs/:id/sessions/...`
  (sessions, session-phases, or program-exercises collections or resources)
- **THEN** `AdminApi` calls `IdentityApi` and evaluates the result before running that
  route's handler

### Requirement: Non-Administrator identities are rejected with 403
For every enforced endpoint except `GET /api/me`, `AdminApi` SHALL inspect the body of
an `IdentityApi` `200` response and respond `403` itself, without calling the
underlying handler, when `role !== "Administrator"`.
**Rationale**: `IdentityApi`'s `200` means "provisioned," not "Administrator" -- it
returns `200` for a `User` identity too. Relaying only `IdentityApi`'s own `401`/`403`
verbatim would let a provisioned `User` through to every Administrator-only endpoint,
since `IdentityApi` never itself returns `403` for "resolved, but wrong role."

#### Scenario: Provisioned User is denied a data endpoint
- **WHEN** `IdentityApi` returns `200` with `role: "User"` for a request to
  `POST /api/programs`
- **THEN** `AdminApi` responds `403` and does not call `createProgram`

#### Scenario: Provisioned Administrator is allowed through
- **WHEN** `IdentityApi` returns `200` with `role: "Administrator"` for a request to any
  enforced endpoint
- **THEN** `AdminApi` runs that endpoint's normal handler and returns its result

### Requirement: IdentityApi's own 401/403 are relayed verbatim
`AdminApi` SHALL relay `IdentityApi`'s own `401` (could not authenticate the request at
all) or `403` (authenticated but unprovisioned or unresolvable role) verbatim to the
caller and SHALL NOT run the underlying handler.
**Rationale**: These are `IdentityApi`'s own denial outcomes, distinct from the
role-mismatch `403` `AdminApi` synthesizes itself; passing them through unchanged keeps
one consistent meaning for each status code reaching the browser.

#### Scenario: Unauthenticated request is rejected
- **WHEN** `IdentityApi` responds `401` to the service-binding call for a request to any
  enforced endpoint
- **THEN** `AdminApi` responds `401` and does not run the underlying handler

#### Scenario: Unprovisioned identity is rejected
- **WHEN** `IdentityApi` responds `403` (no matching `logins`/`users` pair) to the
  service-binding call
- **THEN** `AdminApi` responds `403` and does not run the underlying handler

### Requirement: GET /api/me returns any provisioned identity's role
`AdminApi` SHALL expose `GET /api/me`, which calls `IdentityApi` the same way every
other endpoint does but SHALL NOT apply the Administrator-only check: it returns
`200 { "email": string, "role": "Administrator" | "User" }` for any identity
`IdentityApi` resolves, relaying the `200` body minus the internal `userId` field. It
relays `IdentityApi`'s `401`/`403`/`503` the same as every other endpoint.
**Rationale**: This is the one endpoint a `User`-role caller must be able to reach
successfully, so the client (slice 8c) can distinguish "denied by role" from "not
logged in" -- omitting `userId` keeps an internal identifier out of a response the
browser has no use for.

#### Scenario: Administrator identity via /api/me
- **WHEN** `IdentityApi` returns `200 { "email": "a@x.com", "userId": "1", "role":
  "Administrator" }` for a `GET /api/me` request
- **THEN** `AdminApi` responds `200 { "email": "a@x.com", "role": "Administrator" }`

#### Scenario: User identity via /api/me succeeds, unlike other endpoints
- **WHEN** `IdentityApi` returns `200` with `role: "User"` for a `GET /api/me` request
- **THEN** `AdminApi` responds `200` with that role, not `403`

### Requirement: Created programs are owned by the caller
Every write endpoint that creates a `programs` row (`createProgram`, and any future owner-scoped create) SHALL set `programs.user_id` to the `userId` field from the same `IdentityApi` `200` response already inspected for the role check.
**Rationale**: The response already carries `userId`; a separate D1 lookup to populate
the owner column would duplicate a value already in hand from the enforcement call this
slice already makes.

#### Scenario: New program is owned by the authenticated caller
- **WHEN** an Administrator identity with `userId: "7"` calls `POST /api/programs`
- **THEN** the created `programs` row has `user_id` equal to `"7"`

### Requirement: IdentityApi outages surface as 503, never folded into 403
`AdminApi` SHALL surface a `503` from `IdentityApi`, or a service-binding call that itself throws or times out, as `503`. Any `IdentityApi` response status other than
`200`/`401`/`403`/`503` -- including the `404` `IdentityApi` returns for a
missing/wrong internal key -- SHALL also be treated as `503`. In every one of these
cases, and in the `401`/`403` cases above, `AdminApi` SHALL fail closed: it never runs
the underlying operation unless `IdentityApi` positively returned `200`.
**Rationale**: An infrastructure outage must stay distinguishable from a denied role so
a future UI can show "something went wrong, retry" rather than "no access"; a `404`
here means a misconfigured or rotated internal-key secret, not a real "not found," so
treating it as success or as a routing 404 would either bypass enforcement or mislead
the caller about the actual failure.

#### Scenario: IdentityApi 503 is relayed as 503
- **WHEN** `IdentityApi` responds `503` to the service-binding call
- **THEN** `AdminApi` responds `503` and does not run the underlying handler

#### Scenario: Unexpected IdentityApi status is treated as 503
- **WHEN** `IdentityApi` responds `404` (missing/wrong internal key) to the
  service-binding call
- **THEN** `AdminApi` responds `503`, not `404` and not success

#### Scenario: Service-binding call that throws is treated as 503
- **WHEN** the service-binding call to `IdentityApi` throws or times out instead of
  returning a response
- **THEN** `AdminApi` responds `503` and does not run the underlying handler

### Requirement: Version and OPTIONS are exempt from enforcement
`GET /api/version` and `OPTIONS` preflight requests SHALL bypass `IdentityApi`
enforcement entirely, for every caller.
**Rationale**: `GET /api/version` is polled by `deploy.yaml`'s CI verification step
using a service-token credential with no user JWT to check, and by the client's
same-origin version check; routing either through the per-user JWT check would make the
CI verification step fail every deploy. `OPTIONS` carries no JWT at all.

#### Scenario: Version endpoint is reachable without a JWT
- **WHEN** a request with no `CF_Authorization` cookie reaches `GET /api/version`
- **THEN** `AdminApi` responds normally with the version stamp, making no call to
  `IdentityApi`

#### Scenario: OPTIONS preflight is not enforced
- **WHEN** an `OPTIONS` request reaches any route
- **THEN** `AdminApi` responds with CORS headers and makes no call to `IdentityApi`

### Requirement: Local dev bypasses the service-binding call, not the role check
`AdminApi` SHALL support a `LOCAL_DEV_BYPASS` environment flag, set only in local
`wrangler dev` configuration and never in any deployed configuration. When set,
`AdminApi` SHALL substitute a synthetic local identity
(`{ "email": "local-dev@trainfree.local", "role": "Administrator", "userId": <a real
local users.user_id> }`) for the `IdentityApi` call's result, then continue running each
endpoint's normal handler and Administrator-role check against that synthetic identity
exactly as it would for a real `200` -- including `/api/me`, which relays this identity
to the caller. A deployed environment where the `services` binding or the internal-key
secret is absent SHALL fail closed with `503`; it SHALL NOT fall back to bypass
behavior. The synthetic identity's `userId` SHALL reference a real row inserted by a
local-only setup script into the local D1 instance, never a value seeded by a committed
migration or reachable via any `--remote` path.
**Rationale**: There is no Cloudflare Access edge in front of `wrangler dev`, so no JWT
ever reaches `AdminApi` locally; substituting the identity rather than skipping the
endpoint's own checks keeps local dev exercising the same authorization code path
production runs. Detecting "local" by an explicit flag rather than "binding/key
missing" keeps a production deploy that is accidentally missing either failing closed
instead of silently behaving like local dev. A migration-seeded identity would be a
real, resolvable privileged identity in production too, since `deploy.yaml` applies
every migration file to the production database.

#### Scenario: Local bypass substitutes identity, not the handler
- **WHEN** `LOCAL_DEV_BYPASS` is set and a request reaches `POST /api/programs`
- **THEN** `AdminApi` makes no call to `IdentityApi`, runs `createProgram` using the
  synthetic identity's `userId` as the row's owner, and does not skip the
  Administrator-role check

#### Scenario: Deployed environment missing the binding fails closed
- **WHEN** `LOCAL_DEV_BYPASS` is not set and the `IdentityApi` service binding or
  internal-key secret is absent
- **THEN** `AdminApi` responds `503` to enforced endpoints rather than substituting the
  local identity

#### Scenario: Seed script never runs against the remote database
- **WHEN** the local-only identity seed script is invoked
- **THEN** it writes only to the local D1 instance and is never invoked from
  `deploy.yaml` or any `--remote` command
