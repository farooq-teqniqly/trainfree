# Trainfree Identity -- Slice 1: Trainfree.IdentityApi

See [identity-intent.md](identity-intent.md) for shared context: overall goal, Cloudflare
Access flow, and the database schema.

## Scope

Build `Trainfree.IdentityApi`, a new Cloudflare Worker, with no callers wired up yet. This
is a deliberate exception to this repo's "two apps, one Worker each" convention (see
`CLAUDE.md`): `IdentityApi` is a third Worker, called only via service binding from
`AdminApi` and (later) `WorkoutApi` -- never browser-facing -- centralizing JWT
verification and role lookup so neither app Worker duplicates it. `CLAUDE.md` needs a
documented amendment saying exactly this, added as one of this change's tasks.

`IdentityApi` binds the same physical D1 database as the other Workers, for the
`logins`/`users`/`roles` tables only -- consistent with this repo's existing "one logical
dataset, multiple Workers" pattern. A D1 migration adds those tables plus the
`programs.user_id` column. This database already has one migration history, owned and
applied by `Trainfree.AdminApi`'s existing `wrangler d1 migrations apply` deploy step
(`deploy.yaml`) -- a second, independently-tracked migration history against the same
D1 database from `IdentityApi` would risk the two histories conflicting. This slice's
migration stays under `src/Trainfree.AdminApi/migrations/` and rides that existing
deploy step; `IdentityApi` itself needs no migration step of its own, only its normal
deploy/verify sequence (see below) -- it reads the `logins`/`users`/`roles` tables that
`AdminApi`'s migration history created. `IdentityApi` never reads or writes `programs`
rows; `programs.user_id` is populated and read by `AdminApi` alone (slice 2), the same
way `AdminApi` already owns every other `programs` column -- `IdentityApi`'s role is
limited to identity resolution, not program data access.

There is no in-app provisioning UI in this slice (or this whole change). A user's D1
record (email + role) is added by a provisioning script, run after the email is
whitelisted in Cloudflare Access. That script does not exist in this repo yet -- it is
an explicit deliverable of this slice, not a prerequisite assumed to already exist. It
must be idempotent and failure-safe: it first checks whether a *fully provisioned*
identity -- a `logins` row **and** its paired `users` row -- already exists for the
target `(provider_name, provider_id)`; if so, it does nothing. Otherwise it inserts both
the `logins` row and its paired `users` row inside a single D1 transaction/batch, so a
crash or failure partway through can only ever leave "neither row exists" (the next run
retries cleanly) or "both exist" (the next run's existing-pair check catches it) --
never an orphaned `logins` row with no `users` row, which could never resolve a role and
would be stuck permanently unauthenticatable if the script only checked `logins` for
idempotency (as an earlier version of this requirement did). (An earlier version of this
doc had the script claiming a migration-seeded bootstrap placeholder row for the first
user in an environment -- dropped along with `programs.user_id`'s bootstrap-user
design; see `identity-intent.md`'s schema section.)

## Requirements

- **Rollout order**: a `403` from `IdentityApi` means "authenticated but unprovisioned,"
  and slice 2's `AdminApi` enforcement (including `GET /api/me`) relays that `403`
  verbatim -- so deploying slice 2 before any D1 identity exists locks out every
  administrator, including the operator, with no in-app way to recover (no provisioning
  UI in this change). `GET /api/me` is itself introduced by slice 2, so it can't be the
  smoke check that gates slice 2's own rollout -- the check has to run against
  `IdentityApi` directly, which slice 1 already exposes via its public,
  Access-gated hostname (the same one CI calls for `GET /api/version`). The rollout
  runbook is: (1) deploy slice 1 (`IdentityApi` live, `AdminApi` not yet calling it);
  (2) run the provisioning script against the deployed D1 database, creating at least
  one `Administrator` identity; (3) confirm `/internal/identity` resolves that identity
  to `200`/`Administrator`; only then (4) deploy slice 2 with enforcement enabled. Step
  3 cannot be a plain curl/browser call to `IdentityApi`'s public hostname: that
  hostname is gated by `IdentityApi`'s *own*, separately-configured Access application
  (below), whose edge policy the administrator is not necessarily whitelisted into (only
  `Trainfree.Admin`'s Access application is), so the edge would reject the request
  before `IdentityApi`'s Worker code ever runs -- and even if it didn't, the JWT that
  edge issues would carry `IdentityApi`'s own audience, which fails the named-caller
  audience check regardless (`AdminApi`'s stated caller must present `Trainfree.Admin`'s
  audience specifically). The check must instead go through the service binding, the
  same path `AdminApi` uses in production, bypassing the public edge entirely: run a
  short one-off script via `wrangler dev --remote` (or an equivalent local Wrangler
  session bound to the deployed `IdentityApi` over the real service binding) that sends
  the administrator's own `Trainfree.Admin`-issued JWT (captured from their existing
  browser session's `CF_Authorization` cookie) to `/internal/identity` with
  `X-Trainfree-Caller: admin` and the internal key. If this fails, slice 2 is not
  deployed and the operator is never locked out, since `AdminApi` isn't calling
  `IdentityApi` yet at that point. This is a deploy-runbook task for slice 1/2's
  rollout, not something either Worker can enforce in code.
- The canonical `provider_name` value for Cloudflare Access is the literal string
  `"cloudflare-access"`. Both the provisioning script (slice 1) and `IdentityApi`'s role
  lookup (below) must use this exact, shared value -- defined once (e.g. a constant in a
  shared module both import) rather than typed independently in two places, since a
  spelling mismatch between them would make every otherwise-valid login resolve to `403`.
- `IdentityApi` verifies the Cloudflare Access JWT itself (signature, issuer, audience,
  expiry) against Cloudflare's published JWKS for the Access team domain, rather than
  trusting that a request could only have reached the origin Worker via an
  already-enforced edge policy. The issuer (`iss`) must match the configured Access team
  domain -- signature, audience, and expiry alone don't bind the token to that team, so a
  structurally valid JWT from an unexpected issuer must still be rejected.
- The audience (`aud`) check is against the *specific* caller audience the request
  claims to be from, not an allowlist accepted for every call. Cloudflare Access JWTs
  carry `aud` as an **array** of audience tags, not a single scalar string -- the check
  must verify the named caller's configured audience is a member of that array
  (`aud.includes(expectedAudience)`), never a strict equality comparison against the
  whole claim, which would reject every structurally valid token. Slice 1's test suite
  must include a test using a JWT with a multi-element `aud` array to cover this. The
  service-binding contract (below) requires the calling Worker to state which Access
  application it's calling on behalf of (`AdminApi` always states its own --
  `Trainfree.Admin`'s -- audience; `WorkoutApi` will state `Trainfree.Workout`'s once it
  exists), and `IdentityApi` verifies the named caller's configured audience is present
  in the JWT's `aud` array, not merely that *some* audience on a shared allowlist is
  present. Without this, a valid `Trainfree.Workout` JWT
  for an Administrator-role user could satisfy an accept-any-listed-audience check if it
  ever reached `AdminApi`'s service binding, collapsing the two Access applications'
  boundary and letting a Workout-side identity gain Admin-side access via
  role alone. `AdminApi` forwards the browser's own JWT over the service binding
  unchanged, so that JWT's `aud` is always the calling app's own Access application,
  never `IdentityApi`'s; that's still the reason the check isn't against
  `IdentityApi`'s own hostname/Access application audience.
- The JWKS is fetched from Cloudflare's certs endpoint and cached via the Cloudflare Cache
  API (`caches.default`), respecting Cloudflare's own `Cache-Control`/`max-age` on that
  response -- no new binding needed. Tested by injecting the fetcher as a
  dependency/module seam, so vitest supplies a fake returning a locally-generated JWKS +
  matching test-signed JWTs, with no real network call.
- The identity-extraction code is not tightly coupled to Cloudflare Access specifically:
  it's behind a seam (e.g. a single interface/module), with Cloudflare Access as its only
  implementation in this change. No second provider (e.g. Google) is implemented now --
  this only avoids hardcoding Cloudflare-Access-specific parsing at every call site, and
  centralizes it in one Worker instead of duplicating it per app.
- The service-binding contract: a calling Worker sends a `GET /internal/identity`
  request carrying the `CF_Authorization` cookie/JWT, a required `X-Trainfree-Caller`
  header naming which Access application it's calling on behalf of (`admin` for
  `AdminApi`, `workout` for the future `WorkoutApi`), and a required
  `X-Trainfree-Internal-Key` header. Each caller has its **own** internal-key secret
  (`ADMIN_INTERNAL_KEY`, and later `WORKOUT_INTERNAL_KEY`), stored as a distinct
  Wrangler secret in `IdentityApi` and injected only into its one legitimate caller
  Worker -- not one secret shared across every caller. `IdentityApi` looks up which key
  is expected for the `X-Trainfree-Caller` the request names and checks the presented
  key against *that specific* secret, not against a pool of any-valid-key: a single
  shared secret would let any key-holding Worker claim to be `admin` (`X-Trainfree-
  Caller` is just a self-reported label) and obtain `Trainfree.Admin`-audience
  validation for whatever JWT it forwards, regardless of which Worker actually holds
  the key. The `/internal/identity` *path* is a naming convention only, not an access
  boundary: `IdentityApi` also has a public hostname (so CI can reach
  `GET /api/version`), so any browser or external caller can otherwise reach
  `/internal/identity` directly and would satisfy the JWT/`X-Trainfree-Caller` checks
  just as validly as a real service-bound call, since neither of those proves the
  request came through the service binding rather than the public internet. The
  per-caller key is what actually restricts this endpoint -- `IdentityApi` rejects any
  request to `/internal/identity` missing or presenting the wrong key for the named
  caller with a `404` (not `401`/`403`, so the endpoint's existence isn't confirmed to
  an unauthenticated public prober) before even looking at the JWT -- this `404` uses
  the same stable `application/json { "error": string }` shape as every other non-`200`
  response (below), not an empty or platform-default body, since `AdminApi` (slice 2)
  consumes this response too. Slice 1's test
  suite must include a test asserting a request to `/internal/identity` with a valid
  JWT but no/wrong internal key is rejected, and a test asserting `admin`'s key cannot
  be used to claim `workout` (or vice versa) once `WorkoutApi` exists. `IdentityApi`
  responds with
  `200 { "email": string, "userId": number, "role": "Administrator" | "User" }` only
  when the JWT's `aud` matches the configured audience for the named caller. `userId`
  is included specifically so callers like `AdminApi` can populate owner columns (e.g.
  `programs.user_id`, see slice 1's schema section) without a separate D1 lookup --
  `IdentityApi` already resolved it doing the role lookup. It responds `401` when it cannot
  authenticate the request at all -- missing/malformed `X-Trainfree-Caller`, or a
  missing, malformed, expired, wrong-issuer, or wrong-audience-for-the-named-caller JWT.
  It responds `403` only once authentication has succeeded but authorization fails -- the
  JWT's email has no matching D1 user record, or the record can't be resolved to a role.
  There's no separate "unprovisioned" vs. "wrong role" signal within the `403` case --
  both are a 403; the 401/403 split exists only to distinguish "we don't know who this
  is" from "we know who this is, and they don't have access." Every non-`200` response
  (`401`, `403`, `404`, `503`) is `application/json` with a stable `{ "error": string }`
  body, same as the `200` shape's content type -- never an empty body or plain text. This
  matters because slice 3 treats a non-JSON response from `/api/me` as a sign of an
  expired Access session (see `identity-intent.md`); a denial response that isn't valid
  JSON would be misread as an expired session instead of "no access."
- An infrastructure failure -- the JWKS fetch failing with no cached copy available, or
  the D1 role lookup erroring or timing out -- is surfaced as a `503 Response`, distinct
  from `401`/`403`. `403`/`401` mean "I evaluated this JWT and it is not authorized" /
  "not authenticated"; `503` means "I could not evaluate it at all." This distinction
  matters downstream: slice 3 shows a "something went wrong, retry" state on `5xx`, which
  would be actively misleading if an infrastructure outage looked identical to a denied
  login. `IdentityApi` never lets an unhandled exception propagate as an uncaught `5xx`
  either -- infra failures are caught and turned into an explicit `503`.
- `IdentityApi` looks up the role from D1 fresh on every call (no caching/session of
  role). This keeps a role change effective immediately and avoids cache-invalidation
  complexity; traffic volume is low enough that the extra read is cheap. The lookup
  matches on **both** `provider_name` and `provider_id` together (Cloudflare Access's
  own fixed provider name plus the JWT's email) -- the schema's `UNIQUE (provider_name,
  provider_id)` constraint (see `identity-intent.md`) exists precisely because the same
  `provider_id` string is permitted to recur under a different `provider_name` once a
  second provider exists; matching on the JWT's email alone, without also constraining
  `provider_name` to Cloudflare Access, could select a different provider's `logins`
  row for the same identifier and grant its role instead. Slice 1's test suite must
  include a collision test seeding two `logins` rows with the same `provider_id` under
  different `provider_name`s and asserting the lookup resolves the correct one.
- `IdentityApi` has no Blazor client of its own, but `deploy.yaml` still stamps it with
  `APP_VERSION`/`APP_COMMIT` and polls its own `GET /api/version` in CI after deploy --
  just no client-side comparison/reload banner, since no Blazor app calls it directly. It
  still needs a public URL (gated by Cloudflare Access) purely so CI can reach it;
  browsers never call it. `deploy.yaml` today has exactly one publish/deploy/verify
  sequence (`Trainfree.Admin`/`Trainfree.AdminApi`); this slice's tasks include
  restructuring it to add a second deploy/verify sequence for `IdentityApi` (no publish
  step, since it has no client), each Worker with its own `APP_BASE_URL`-equivalent and
  Cloudflare Access service token wiring.
- `IdentityApi`'s public hostname is gated by its own, separately-configured Cloudflare
  Access application in the Zero Trust dashboard (manual setup, same as the existing
  `Trainfree.Admin` application) -- distinct from `Trainfree.Admin`'s, since it's a
  different hostname. This is a one-time manual setup task for this slice, not
  represented as code, per this repo's existing "Cloudflare Access is configured
  manually" convention.
