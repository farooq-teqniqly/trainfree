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
`programs.user_id` column.

There is no in-app provisioning UI in this slice (or this whole change). A user's D1
record (email + role) is added by a script, run after the email is whitelisted in
Cloudflare Access.

## Requirements

- `IdentityApi` verifies the Cloudflare Access JWT itself (signature, audience, expiry)
  against Cloudflare's published JWKS for the Access team domain, rather than trusting
  that a request could only have reached the origin Worker via an already-enforced edge
  policy.
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
  carrying the `CF_Authorization` cookie/JWT. `IdentityApi` responds with
  `200 { "email": string, "role": "Administrator" | "User" }` on success, or a
  `403 Response` when the JWT's email has no matching D1 user record or the record can't
  be resolved to a role. There's no separate "not provisioned" vs. "wrong role" signal in
  this version -- both are a 403.
- `IdentityApi` looks up the role from D1 fresh on every call (no caching/session of
  role). This keeps a role change effective immediately and avoids cache-invalidation
  complexity; traffic volume is low enough that the extra read is cheap.
- `IdentityApi` has no Blazor client of its own, but `deploy.yaml` still stamps it with
  `APP_VERSION`/`APP_COMMIT` and polls its own `GET /api/version` in CI after deploy, same
  safety net as the other Workers -- just no client-side comparison/reload banner, since
  no Blazor app calls it directly. It still needs a public URL (gated by Cloudflare
  Access) purely so CI can reach it; browsers never call it.
