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
deploy/verify sequence (see below) -- it reads tables that `AdminApi`'s migration
history created, the same way it will read `programs` rows `AdminApi` created.

There is no in-app provisioning UI in this slice (or this whole change). A user's D1
record (email + role) is added by a provisioning script, run after the email is
whitelisted in Cloudflare Access. That script does not exist in this repo yet -- it is
an explicit deliverable of this slice, not a prerequisite assumed to already exist. It
must be idempotent: it first checks whether a `logins` row already exists for the
target `(provider_name, provider_id)` and, if so, does nothing; otherwise it inserts a
new `logins`/`users` row. That existing-row check first is what makes re-running the
script for an already-provisioned identity a no-op instead of a duplicate insert. (An
earlier version of this doc had the script claiming a migration-seeded bootstrap
placeholder row for the first user in an environment -- dropped along with
`programs.user_id`'s bootstrap-user design; see `identity-intent.md`'s schema section.)

## Requirements

- `IdentityApi` verifies the Cloudflare Access JWT itself (signature, issuer, audience,
  expiry) against Cloudflare's published JWKS for the Access team domain, rather than
  trusting that a request could only have reached the origin Worker via an
  already-enforced edge policy. The issuer (`iss`) must match the configured Access team
  domain -- signature, audience, and expiry alone don't bind the token to that team, so a
  structurally valid JWT from an unexpected issuer must still be rejected.
- The audience (`aud`) check is against the *specific* caller audience the request
  claims to be from, not an allowlist accepted for every call. The service-binding
  contract (below) requires the calling Worker to state which Access application it's
  calling on behalf of (`AdminApi` always states its own -- `Trainfree.Admin`'s -- audience;
  `WorkoutApi` will state `Trainfree.Workout`'s once it exists), and `IdentityApi`
  verifies the JWT's `aud` matches *that* stated audience, not merely that it matches
  *some* audience on a shared allowlist. Without this, a valid `Trainfree.Workout` JWT
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
- The service-binding contract: a calling Worker sends a plain `fetch`-style request
  carrying the `CF_Authorization` cookie/JWT and a required `X-Trainfree-Caller` header
  naming which Access application it's calling on behalf of (`admin` for `AdminApi`,
  `workout` for the future `WorkoutApi`). `IdentityApi` responds with
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
  is" from "we know who this is, and they don't have access."
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
  complexity; traffic volume is low enough that the extra read is cheap.
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
