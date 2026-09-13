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

- `logins` -- one row per external identity (`provider_id` + `provider_name`, e.g. the
  Cloudflare Access email as `provider_id`). A second future provider is just another
  `logins` row shape, no schema change.
- `users` -- surrogate `user_id`, 1:1 to `logins` via `login_id`, many:1 to `roles` via
  `role_id`.
- `roles` -- a lookup table of `Administrator`/`User` rows, not a raw enum column.
- `programs.user_id` -- links each program to its owning user. **Nullable**, no `NOT
  NULL` and no bootstrap-user complexity: existing `programs` rows get `NULL` (meaning
  "no owner yet -- predates identity"), and stay `NULL` until slice 2's write path
  starts supplying a real `user_id` on create. This is a deliberate simplification over
  an earlier NOT-NULL-with-bootstrap-user design that required a placeholder
  `logins`/`users` row, a claim/idempotency script, and a multi-table FK-graph rebuild
  to add the column safely under D1's enforced foreign keys -- none of that machinery
  is needed once the column can just start `NULL`.
  The column carries no `REFERENCES` clause at the schema level: D1 enforces foreign
  keys, and SQLite rejects adding a column with a `REFERENCES` clause via
  `ALTER TABLE ... ADD COLUMN` while FK enforcement is on, regardless of nullability --
  so a schema-level FK here would still force the same table-rebuild this
  simplification is meant to avoid. `programs.user_id` is therefore a plain nullable
  `INTEGER`, added via a single `ALTER TABLE programs ADD COLUMN user_id INTEGER;`, and
  referential integrity to `users.user_id` is enforced in `AdminApi`'s application code
  at write time (slice 2), not by the schema. No table rebuild, no dependent-table
  changes, no bootstrap row, no provisioning-script idempotency requirement tied to
  this migration.

The [issue #66](https://github.com/farooq-teqniqly/trainfree/issues/66) JWT contains the
user's email as `email`, used as `provider_id`.
