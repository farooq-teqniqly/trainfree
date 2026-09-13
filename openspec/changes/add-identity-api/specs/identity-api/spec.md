## Purpose

A standalone Cloudflare Worker that centralizes Cloudflare Access JWT verification and
D1-backed role lookup for every Trainfree app Worker, reached only over a service
binding, so no app Worker duplicates identity logic.

## ADDED Requirements

### Requirement: IdentityApi is a service-binding-only third Worker
`Trainfree.IdentityApi` SHALL exist as its own deployable Cloudflare Worker, callable
only via service binding from app Workers (`AdminApi` now, `WorkoutApi` later), never
directly by a browser.
**Rationale**: This is a deliberate exception to the repo's "two apps, one Worker each"
convention -- centralizing JWT verification and role lookup in one place avoids
duplicating that logic per app Worker.

#### Scenario: IdentityApi deployed with no callers wired up
- **WHEN** `IdentityApi` is deployed as part of this change
- **THEN** it is live and independently testable, while no other Worker yet calls it

### Requirement: IdentityApi binds the shared D1 database for identity tables only
`IdentityApi` SHALL bind the same physical D1 database (`trainfree_db`) as the other
Workers, reading and writing only the `logins`, `users`, and `roles` tables. It SHALL
NOT read or write `programs` rows.
**Rationale**: One logical dataset across Workers avoids data duplication or a sync
step; `programs` ownership stays exclusively with `AdminApi`, consistent with its
existing sole ownership of every other `programs` column.

#### Scenario: IdentityApi never touches programs rows
- **WHEN** `IdentityApi` resolves an identity's role
- **THEN** it queries only `logins`, `users`, and `roles`, and issues no read or write
  against `programs`

### Requirement: Identity schema migration rides AdminApi's existing migration history
The identity schema migration SHALL live under `src/Trainfree.AdminApi/migrations/` and
apply via `AdminApi`'s existing `wrangler d1 migrations apply` deploy step, creating
`logins`, `users`, and `roles` and adding the `programs.user_id` column. `IdentityApi`
itself SHALL have no migration step of its own.
**Rationale**: The D1 database already has one migration history owned by `AdminApi`;
a second, independently tracked history against the same database risks the two
histories conflicting.

#### Scenario: Migration applies under AdminApi's deploy step
- **WHEN** `deploy.yaml` runs `AdminApi`'s `wrangler d1 migrations apply` step
- **THEN** the `logins`, `users`, and `roles` tables exist and `programs` has a
  `user_id` column, with no separate migration step having run for `IdentityApi`

### Requirement: Provisioning script is idempotent and failure-safe
A provisioning script SHALL add a user's D1 identity (email + role) after the email is
whitelisted in Cloudflare Access. It SHALL check whether a fully provisioned identity
-- a `logins` row **and** its paired `users` row -- already exists for the target
`(provider_name, provider_id)`, doing nothing if so. Otherwise it SHALL insert both
rows inside a single D1 transaction/batch.
**Rationale**: Checking only `logins` for idempotency could leave an orphaned `logins`
row with no paired `users` row, which can never resolve a role and would be
permanently unauthenticatable; checking the pair and writing both rows atomically
means a crash leaves either "neither row exists" (safe to retry) or "both exist"
(caught by the next run's check), never the orphaned state.

#### Scenario: Re-running the script for an existing identity is a no-op
- **WHEN** the provisioning script runs for a `(provider_name, provider_id)` that
  already has both a `logins` row and its paired `users` row
- **THEN** it makes no D1 writes

#### Scenario: Script writes both rows atomically
- **WHEN** the provisioning script runs for a `(provider_name, provider_id)` with no
  existing identity
- **THEN** it inserts the `logins` row and its paired `users` row in a single
  transaction/batch, so a failure partway through leaves neither row present

### Requirement: Canonical provider_name constant is shared
The literal string `"cloudflare-access"` SHALL be defined once, in a shared module,
and used by both the provisioning script and `IdentityApi`'s role lookup as the
`provider_name` for Cloudflare Access identities.
**Rationale**: A spelling mismatch between the two independent call sites would make
every otherwise-valid login resolve to `403`.

#### Scenario: Provisioning script and lookup use the same constant
- **WHEN** the provisioning script inserts a `logins` row for a Cloudflare Access
  identity
- **THEN** its `provider_name` value is produced by the same shared constant
  `IdentityApi`'s role lookup uses to match against it

### Requirement: JWT verification is independent of edge enforcement
`IdentityApi` SHALL verify the Cloudflare Access JWT's signature, issuer, audience, and
expiry itself, against Cloudflare's published JWKS for the configured Access team
domain, rather than trusting that a request could only have reached it via an
already-enforced edge policy. The issuer (`iss`) SHALL be checked against the
configured Access team domain.
**Rationale**: Signature, audience, and expiry alone don't bind the token to a specific
team domain; a structurally valid JWT from an unexpected issuer must still be rejected.

#### Scenario: JWT with wrong issuer is rejected
- **WHEN** a request presents a structurally valid, correctly signed JWT whose `iss`
  does not match the configured Access team domain
- **THEN** `IdentityApi` responds `401`

### Requirement: Audience check is against the named caller's specific audience
The service-binding request SHALL state which Access application it's calling on
behalf of via `X-Trainfree-Caller` (`admin` for `AdminApi`, `workout` for the future
`WorkoutApi`). `IdentityApi` SHALL verify that named caller's configured audience is a
member of the JWT's `aud` array (`aud.includes(expectedAudience)`), never a strict
equality comparison against the whole `aud` claim, and never acceptance of any
audience from a shared allowlist.
**Rationale**: Cloudflare Access JWTs carry `aud` as an array, so equality against the
whole claim would reject every valid token. Checking only "some listed audience is
present" would let a valid `Trainfree.Workout` JWT satisfy `AdminApi`'s service
binding, collapsing the two Access applications' boundary and letting a Workout-side
identity gain Admin-side access via role alone.

#### Scenario: Multi-element aud array containing the named caller's audience succeeds
- **WHEN** a request names caller `admin` and presents a JWT whose `aud` array
  contains `Trainfree.Admin`'s configured audience alongside other audience values
- **THEN** `IdentityApi` accepts the audience check for that request

#### Scenario: Wrong-audience JWT for the named caller is rejected
- **WHEN** a request names caller `admin` but presents a JWT whose `aud` array does
  not contain `Trainfree.Admin`'s configured audience (e.g. it contains only
  `Trainfree.Workout`'s)
- **THEN** `IdentityApi` responds `401`

### Requirement: JWKS is fetched and cached via the Cloudflare Cache API
`IdentityApi` SHALL fetch the JWKS from Cloudflare's certs endpoint and cache it via
the Cache API (`caches.default`), respecting the response's own `Cache-Control`/
`max-age`, with no new binding required. The fetcher SHALL be injected as a
dependency/module seam so it can be replaced in tests with a fake JWKS and matching
test-signed JWTs, with no real network call in tests.
**Rationale**: Reusing Cloudflare's own cache semantics avoids inventing a second
freshness policy; the injected seam keeps the test suite free of real network
dependencies.

#### Scenario: Cached JWKS is reused within its max-age
- **WHEN** a second request arrives before the cached JWKS response's `max-age`
  expires
- **THEN** `IdentityApi` verifies the JWT signature using the cached JWKS without a
  new fetch to Cloudflare's certs endpoint

### Requirement: Identity extraction is behind a provider seam
The code that extracts identity from a request SHALL sit behind a single
interface/module seam, with Cloudflare Access as its only implementation in this
change.
**Rationale**: No second provider (e.g. Google) is implemented now; the seam only
avoids hardcoding Cloudflare-Access-specific parsing at every call site and
centralizes it in one Worker instead of duplicating it per app.

#### Scenario: Cloudflare Access is the sole registered provider
- **WHEN** `IdentityApi` extracts identity from an incoming request
- **THEN** it does so through the provider seam's Cloudflare Access implementation,
  with no call site parsing Access-specific claims directly

### Requirement: Service-binding contract for /internal/identity
`IdentityApi` SHALL expose `GET /internal/identity`, requiring the `CF_Authorization`
cookie/JWT, a required `X-Trainfree-Caller` header naming the calling Access
application (`admin` or `workout`), and a required `X-Trainfree-Internal-Key` header.
Each caller SHALL have its own distinct internal-key secret (`ADMIN_INTERNAL_KEY`,
`WORKOUT_INTERNAL_KEY`), and `IdentityApi` SHALL check the presented key against only
the secret configured for the named caller.
**Rationale**: `X-Trainfree-Caller` is a self-reported label; a single shared secret
would let any key-holding Worker claim to be `admin` and obtain `Trainfree.Admin`
audience validation regardless of which Worker actually holds the key.

#### Scenario: Correct caller-specific key succeeds
- **WHEN** a request names caller `admin` and presents the key configured as
  `ADMIN_INTERNAL_KEY`, alongside a JWT valid for `Trainfree.Admin`'s audience
- **THEN** `IdentityApi` proceeds to JWT verification and role lookup

#### Scenario: Admin's key cannot be used to claim workout
- **WHEN** a request names caller `workout` but presents the key configured as
  `ADMIN_INTERNAL_KEY`
- **THEN** `IdentityApi` responds `404` without evaluating the JWT

### Requirement: Missing or wrong internal key responds 404, checked before the JWT
`IdentityApi` SHALL respond `404` (not `401`/`403`) to a request to `/internal/identity`
with a missing or wrong internal key for the named caller, checked before the JWT is
evaluated at all. The `/internal/identity` path itself is a naming convention only, not
an access boundary -- `IdentityApi` also has a public hostname reachable by any caller.
**Rationale**: A `404` avoids confirming the endpoint's existence to an unauthenticated
public prober; the per-caller key, not the path or JWT presence, is what actually
restricts the endpoint, since neither the path nor a valid-looking JWT proves the
request came through the service binding rather than the public internet.

#### Scenario: Valid JWT with missing internal key still yields 404
- **WHEN** a request to `/internal/identity` presents a valid, correctly audienced
  JWT but omits `X-Trainfree-Internal-Key`
- **THEN** `IdentityApi` responds `404` with the standard `{ "error": string }` JSON
  body, without inspecting the JWT

### Requirement: Response shapes and status codes are stable JSON
On success, `IdentityApi` SHALL respond `200 { "email": string, "userId": number,
"role": "Administrator" | "User" }`. It SHALL respond `401` when it cannot
authenticate the request at all (missing/malformed `X-Trainfree-Caller`, or a missing,
malformed, expired, wrong-issuer, or wrong-audience-for-the-named-caller JWT). It
SHALL respond `403` only once authentication has succeeded but the JWT's email has no
matching D1 user record or the record can't be resolved to a role, with no further
distinction between "unprovisioned" and "wrong role." Every non-`200` response
(`401`, `403`, `404`, `503`) SHALL be `application/json` with a stable
`{ "error": string }` body.
**Rationale**: `userId` lets callers like `AdminApi` populate owner columns (e.g.
`programs.user_id`) without a separate D1 lookup. A stable JSON error shape on every
non-200 response matters because slice 3 treats a non-JSON response from `/api/me` as
an expired-Access-session signal; a denial response that isn't valid JSON would be
misread as an expired session instead of "no access."

#### Scenario: Successful resolution returns email, userId, and role
- **WHEN** a correctly keyed, correctly audienced request resolves to a provisioned
  identity
- **THEN** `IdentityApi` responds `200` with `email`, `userId`, and `role` in the body

#### Scenario: Authenticated but unprovisioned identity returns 403
- **WHEN** a request passes key and JWT verification but the JWT's email has no
  matching `logins`/`users` pair
- **THEN** `IdentityApi` responds `403` with the standard JSON error body

### Requirement: Role lookup matches provider_name and provider_id together
`IdentityApi`'s role lookup SHALL match a `logins` row on **both** `provider_name` and
`provider_id` together, never on `provider_id` (email) alone.
**Rationale**: The schema's `UNIQUE (provider_name, provider_id)` constraint exists
because the same `provider_id` string is permitted to recur under a different
`provider_name` once a second provider exists; matching on email alone could select a
different provider's `logins` row for the same identifier and grant its role instead.

#### Scenario: Same provider_id under different provider_name resolves the correct row
- **WHEN** two `logins` rows share the same `provider_id` but have different
  `provider_name` values, and a request's JWT resolves to the Cloudflare Access
  identity
- **THEN** `IdentityApi`'s role lookup returns the role from the `logins` row whose
  `provider_name` is `"cloudflare-access"`, not the other row

### Requirement: Infrastructure failures surface as 503, never an uncaught 5xx
`IdentityApi` SHALL surface a JWKS fetch failure with no cached copy available, or a D1
role lookup error or timeout, as `503`, distinct from `401`/`403`, and SHALL never let
an unhandled exception propagate as an uncaught `5xx`.
**Rationale**: `401`/`403` mean "evaluated and not authorized/authenticated"; `503`
means "could not evaluate at all." Slice 3 shows a "something went wrong, retry" state
on `5xx`, which would be misleading if an infrastructure outage looked identical to a
denied login.

#### Scenario: JWKS fetch failure with no cache yields 503
- **WHEN** the JWKS fetch fails and no cached JWKS is available
- **THEN** `IdentityApi` responds `503` with the standard JSON error body

#### Scenario: D1 lookup error yields 503
- **WHEN** the D1 role lookup query errors or times out
- **THEN** `IdentityApi` responds `503` with the standard JSON error body, not an
  uncaught exception

### Requirement: Role lookup has no caching
`IdentityApi` SHALL look up the role from D1 fresh on every call, with no
caching/session of role between requests.
**Rationale**: This keeps a role change effective immediately and avoids
cache-invalidation complexity; traffic volume is low enough that the extra read is
cheap.

#### Scenario: Role change is effective on the next call
- **WHEN** a user's role is changed in D1 between two `/internal/identity` requests
  for that user
- **THEN** the second request's response reflects the new role

### Requirement: IdentityApi is stamped and polled in CI with no client banner
`deploy.yaml` SHALL stamp `IdentityApi` with `APP_VERSION`/`APP_COMMIT` and poll its
`GET /api/version` in CI after deploy, the same as `AdminApi`. `IdentityApi` SHALL have
no client-side stamp comparison or reload banner, since no Blazor app calls it
directly. It SHALL have a public hostname, gated by its own Cloudflare Access
application, solely so CI can reach it -- browsers never call it otherwise.
**Rationale**: `deploy.yaml` today has exactly one publish/deploy/verify sequence;
this slice adds a second deploy/verify sequence for `IdentityApi` with no publish step,
since it has no client to publish.

#### Scenario: CI verifies IdentityApi's deployed stamp
- **WHEN** `deploy.yaml` deploys `IdentityApi` and then polls its `GET /api/version`
- **THEN** the reported stamp matches the `<tag>+<short-sha>` stamp injected into that
  deploy, and the job fails if it does not

## Decisions

- **Third Worker vs. duplicating identity logic per app** (Scope): Chose a dedicated
  `IdentityApi` Worker reached over service bindings, over implementing JWT
  verification and role lookup independently inside `AdminApi` and (later)
  `WorkoutApi`. Duplication would double the code that has to get token verification
  and audience scoping right, and could drift between the two apps. This is worth
  revisiting only if Cloudflare service bindings ever became unavailable or
  prohibitively costly between Workers, which is not the case today.

- **404 (not 401/403) for a missing/wrong internal key** (service-binding contract):
  Chose `404` specifically so an unauthenticated public prober hitting
  `/internal/identity` directly cannot distinguish "endpoint exists, wrong key" from
  "no such route." A `401`/`403` would confirm the endpoint's existence and invite
  further probing. This would be worth relaxing only if `/internal/identity` were ever
  moved behind a network boundary the public internet genuinely cannot reach (e.g. a
  binding-only route with no public hostname at all), which isn't possible while
  `IdentityApi` also needs a public hostname for CI's `GET /api/version` check.

- **Email as provider_id, not JWT sub, for the Cloudflare Access provider** (schema,
  from `identity-intent.md`): Chose the whitelisted email over the JWT's `sub` claim
  as `provider_id`, even though `sub` is more stable (immune to email reuse/changes),
  because keying on `sub` would require a two-phase provisioning flow (a pending
  record by email, bound to `sub` on first login) that this change does not build.
  Accepted risk: a changed or reassigned email could bind a new person to a previous
  user's history. This is acceptable for a self-hosted, single-operator app with a
  handful of manually whitelisted users, and should be revisited if a second identity
  provider or a larger user base changes that calculus.

- **No role caching** (role lookup): Chose a fresh D1 read on every
  `/internal/identity` call over caching the role per session or per JWT, trading a
  small amount of latency for immediate effect of role changes and no
  cache-invalidation logic. Worth revisiting only if D1 read volume/latency from this
  path becomes a measured problem, which is not expected at this app's traffic volume.

- **Rollout order and its smoke check** (Requirements, rollout order): The
  slice-1-then-2 deploy order, and the requirement that slice 2's rollout smoke check
  go through the real service binding via a dedicated smoke-harness Wrangler config
  (not `AdminApi`'s own local config, which has `LOCAL_DEV_BYPASS` set), is captured
  here as a decision rather than a testable requirement of `IdentityApi` itself: it is
  a deploy-runbook procedure for slice 1/2's rollout, not something `IdentityApi`'s own
  code enforces or that this slice's automated test suite can verify. No alternative
  was seriously considered, since skipping the smoke check risks locking out the
  operator with no in-app recovery path.

## Requirement coverage

Anchor: docs/identity/identity-intent-01-identityapi.md (frozen intent doc, "Scope" and
"Requirements" sections)

| # | Anchor requirement | Covered by |
|---|--------------------|-----------|
| 1 | `IdentityApi` is a new Worker, service-binding-only, no callers wired up yet (Scope) | Req: IdentityApi is a service-binding-only third Worker |
| 2 | `CLAUDE.md` needs a documented amendment for this exception (Scope) | Not covered by spec -- this is a documentation task, tracked in tasks.md |
| 3 | `IdentityApi` binds shared D1 for `logins`/`users`/`roles` only, never `programs` (Scope) | Req: IdentityApi binds the shared D1 database for identity tables only |
| 4 | Migration lives under `AdminApi`'s migration history, not a second independent one (Scope) | Req: Identity schema migration rides AdminApi's existing migration history |
| 5 | Provisioning script is idempotent on the fully-provisioned pair and writes both rows atomically (Scope) | Req: Provisioning script is idempotent and failure-safe |
| 6 | Rollout order/runbook and its dedicated smoke-harness check (Requirements, rollout order) | Decisions: Rollout order and its smoke check -- deploy-runbook procedure, not testable `IdentityApi` behavior; tracked as a task |
| 7 | Canonical `provider_name` = `"cloudflare-access"`, defined once and shared | Req: Canonical provider_name constant is shared |
| 8 | JWT verified against JWKS: signature, issuer (team domain), audience, expiry | Req: JWT verification is independent of edge enforcement |
| 9 | Audience check against the named caller's specific audience within the `aud` array | Req: Audience check is against the named caller's specific audience |
| 10 | JWKS fetched from Cloudflare certs endpoint, cached via Cache API, fetcher injectable for tests | Req: JWKS is fetched and cached via the Cloudflare Cache API |
| 11 | Identity extraction behind a provider seam, Cloudflare Access as sole implementation | Req: Identity extraction is behind a provider seam |
| 12 | Service-binding contract: `GET /internal/identity`, `X-Trainfree-Caller`, per-caller `X-Trainfree-Internal-Key` | Req: Service-binding contract for /internal/identity |
| 13 | Missing/wrong internal key rejected with `404` before JWT evaluation | Req: Missing or wrong internal key responds 404, checked before the JWT |
| 14 | `200`/`401`/`403` response shapes, all non-200 responses stable JSON `{ "error": string }` | Req: Response shapes and status codes are stable JSON |
| 15 | Role lookup matches on `provider_name` and `provider_id` together, not email alone | Req: Role lookup matches provider_name and provider_id together |
| 16 | Infrastructure failures (JWKS fetch, D1 error/timeout) surfaced as `503`, never uncaught `5xx` | Req: Infrastructure failures surface as 503, never an uncaught 5xx |
| 17 | No role caching -- fresh D1 lookup on every call | Req: Role lookup has no caching |
| 18 | `deploy.yaml` stamps/polls `IdentityApi` in CI, no client banner, public hostname for CI only | Req: IdentityApi is stamped and polled in CI with no client banner |
| 19 | `IdentityApi`'s own, separately configured Cloudflare Access application (manual setup) | Not covered by spec -- one-time manual Zero Trust dashboard setup, not code; tracked as a task per the repo's existing "Cloudflare Access is configured manually" convention |
