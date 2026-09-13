# Trainfree Identity

Currently, Trainfree Admin is a single-user application. This document describes the
intent for adding Identity to Trainfree Admin. The goal of Identity is to support
multiple users for not only Trainfree Admin but the future Trainfree Workout app
(`Trainfree.Workout`/`Trainfree.WorkoutApi` don't exist yet -- roadmap slice 8+ -- so
they're context here, not built by any of the slices below).

This work is split into three sequential slices, each its own OpenSpec change. Order is a
hard dependency chain: slice 2 can't enforce anything without slice 1 deployed, and slice
3's UI would just wrap an unenforced API without slice 2.

1. [`Trainfree.IdentityApi`](identity-intent-01-identityapi.md) -- the Worker itself: JWT
   verification, D1 schema, service-binding contract. Deployable and testable standalone,
   with no callers yet.
2. [`AdminApi` enforcement](identity-intent-02-adminapi-enforcement.md) -- wires every
   `Trainfree.AdminApi` endpoint to call `IdentityApi` and enforce the Administrator role.
   Closes the security gap even before any UI changes.
3. [`Trainfree.Admin` access gating](identity-intent-03-admin-access-gating.md) -- startup
   `/api/me` check and the "no access" page. Pure UX on top of an already-secure API.

## Constraints

Access to Trainfree Admin is gated by Cloudflare Access. This will continue to be the
case. At this time, providers such as Google don't need to be supported -- see slice 1's
provider-abstraction requirement for how far that decoupling goes now.

## Cloudflare Access

Currently, when one browses to Trainfree Admin's Worker URL and is not authenticated,
Cloudflare Access presents a login form that asks for an email address. When the form is
submitted, an access code is sent to the email address, and the login form presents an
input box where the access code should be entered. Once the access code is submitted and
verified, Cloudflare Access issues a session, and the Trainfree Admin home page is shown.

The email address needs to be whitelisted in Cloudflare Access for authentication to
succeed. All administrators and non-administrators will need to be added manually to
Cloudflare Access. This is sufficient for the first version as it will only be open to a
handful of users.

If the JWT expires while the user is logged in, Cloudflare Access redirects the user to
its login page for re-authentication. This is existing Cloudflare Access behavior -- no
Trainfree-side handling is needed in any slice.

## Database schema

Identity introduces `logins`, `users`, and `roles` tables, plus a `programs.user_id`
column, in the [proposed schema](https://lucid.app/lucidchart/e74e6f97-b0f1-47a2-ac12-d8c01765bfc7):

- `logins` -- one row per external identity (`provider_id` + `provider_name`). For
  Cloudflare Access, `provider_id` is the whitelisted **email**, not the JWT's `sub`
  claim -- a deliberate choice, not an oversight: `sub` is the objectively more stable
  key (immune to email changes/reuse), but keying on it would require a two-phase
  provisioning flow (create a pending-by-email record before the user's first login,
  then bind `sub` on their first authenticated request), which this change explicitly
  doesn't build. Email-as-key means a changed or reassigned email could bind a new
  person to a previous user's `programs`/history; this repo accepts that risk for v0.1
  because it's a self-hosted app with a handful of manually-whitelisted users under one
  operator's control (`CLAUDE.md`'s "self-hosted, single-user" framing, extended here to
  "single-operator, few users") -- not a multi-tenant SaaS where an attacker could
  register a freed email. Revisit if a second identity provider or a larger user base
  changes that calculus. A second future provider is just another `logins` row shape,
  no schema change. `UNIQUE (provider_name, provider_id)` at the schema level -- the
  provisioning script's own "does this pair already exist" check (slice 1) is
  read-then-write and not race-proof on its own; the constraint is what actually
  guarantees the pair identifies at most one row, and turns a racing double-run into a
  constraint-violation error on the loser rather than a silent duplicate.
- `users` -- surrogate `user_id`, 1:1 to `logins` via `login_id`, many:1 to `roles` via
  `role_id`. `UNIQUE (login_id)` enforces the 1:1 at the schema level (without it,
  nothing stops two `users` rows pointing at the same `logins` row, which would make
  the role lookup for that login ambiguous).
- `roles` -- a lookup table of `Administrator`/`User` rows, not a raw enum column. The
  migration that creates this table also seeds exactly those two rows -- the
  provisioning script only ever looks up an existing role by name to get its
  `role_id`, it never creates one, so a fresh database with an empty `roles` table
  would leave every identity unable to resolve a role and stuck at `403`.
- `programs.user_id` -- links each program to its owning user, `INTEGER REFERENCES
  users(user_id)`, **nullable** (no `NOT NULL`, no default -- an omitted default is
  `NULL`). SQLite/D1 does allow adding a nullable FK column with no default via a
  single `ALTER TABLE programs ADD COLUMN user_id INTEGER REFERENCES users(user_id);`
  even with foreign keys enforced -- the earlier restriction this doc cited (rejecting
  `ADD COLUMN` with a `REFERENCES` clause under FK enforcement) only applies when
  combined with `NOT NULL`/a non-`NULL` default, not to a plain nullable FK column. So
  the FK constraint is kept: dropping it (as an earlier revision of this doc did) would
  leave `programs.user_id` capable of holding an orphaned/invalid `user_id` with nothing
  in the schema to prevent it, pushing that guarantee onto every future writer instead.
  This is still a real simplification over the original NOT-NULL-with-bootstrap-user
  design: existing `programs` rows just get `NULL` (meaning "no owner yet -- predates
  identity") with a single-statement migration, no bootstrap row, no claim script, no
  table rebuild. `user_id` stays `NULL` until slice 2's write path starts supplying it
  on create -- slice 2 must be able to populate it, which requires `IdentityApi` to
  return the caller's `user_id`, not just `email`/`role` (see slice 1's contract).

The [issue #66](https://github.com/farooq-teqniqly/trainfree/issues/66) JWT contains the
user's email as `email`, used as `provider_id` (see the `logins` note above for why
email rather than `sub`).
