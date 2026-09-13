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
- `programs.user_id` -- links each program to its owning user, `NOT NULL REFERENCES
  users(user_id) DEFAULT 1`. SQLite/D1 rejects adding a column with a `REFERENCES`
  clause via `ALTER TABLE ... ADD COLUMN` while foreign keys are enforced (D1 enforces
  them, and this repo's existing migrations declare FKs the same way, e.g.
  `src/Trainfree.AdminApi/migrations/0003_create_sessions.sql:5`) -- so this migration
  does a table rebuild, following SQLite's documented "12 steps" pattern for changing a
  table referenced by other tables' foreign keys: `PRAGMA foreign_keys=OFF`, create the
  new `programs` table with the `user_id` column and its `DEFAULT 1`/FK included from
  the start, copy every existing row across (each row automatically gets `user_id = 1`
  via the default), drop the old table, rename the new one to `programs`, recreate
  `idx_programs_name_nocase` (migration 0002) against the new table, then
  `PRAGMA foreign_keys=ON` and a `PRAGMA foreign_key_check` before committing. Turning
  foreign keys off for the rebuild -- rather than a bare `DROP TABLE` -- is what keeps
  `sessions.program_id`'s `ON DELETE CASCADE` from firing and cascading through
  `sessions`, `session_phases`, and `program_exercises` when the old `programs` table is
  dropped; those tables and their data are otherwise untouched by this migration. Every
  `createProgram` insert that doesn't yet supply `user_id` (i.e. until slice 2 updates
  that write path) also resolves to `1` by the same default -- no row is ever left with
  an unresolved owner, and slice 1 shipping alone does not break `AdminApi`'s existing
  `createProgram` path.
  The bootstrap `logins`/`users` row that `user_id = 1` refers to is *not* seeded with a
  real email in this committed migration -- a deployment-specific personal email baked
  into checked-in SQL would be wrong for every other environment this migration runs
  against (a second self-hosted deploy, a test D1 instance), and re-running migrations
  can't change it afterward. Instead the migration inserts one placeholder `logins` row
  (`provider_name = 'bootstrap'`, `provider_id = 'bootstrap-placeholder'`) and its
  `users` row (`user_id = 1`, Administrator role). The provisioning script (see slice 1;
  this doc previously implied the script already existed -- it's an explicit slice-1
  deliverable, not yet built) is how the real operator claims that identity per
  deployment: for the very first user provisioned in a given environment, the script
  performs an idempotent claim -- if a `logins` row with `provider_name = 'bootstrap'`
  still exists, update *both* `provider_name` and `provider_id` to the operator's real
  provider name (`"cloudflare-access"`) and email in place, rather than inserting a new
  row (leaving `provider_name` as `'bootstrap'` would make every subsequent lookup by
  the real `(provider_name, provider_id)` pair fail to find this row, rejecting the
  operator as unprovisioned). If no `'bootstrap'` row remains (already claimed), the
  script inserts a normal new `logins`/`users` row instead -- so re-running the script
  is always safe. `user_id = 1` (and therefore every pre-existing `programs` row) ends
  up owned by whichever identity the operator of that specific deployment provisions
  first.

The [issue #66](https://github.com/farooq-teqniqly/trainfree/issues/66) JWT contains the
user's email as `email`, used as `provider_id`.
