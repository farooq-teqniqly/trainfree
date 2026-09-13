# Trainfree Identity -- Slice 3: Trainfree.Admin access gating

See [identity-intent.md](identity-intent.md) for shared context and
[identity-intent-02-adminapi-enforcement.md](identity-intent-02-adminapi-enforcement.md)
for the `GET /api/me` endpoint this slice calls. This is pure UX on top of an
already-secure API -- `AdminApi` already rejects unauthorized requests without this slice.

## Scope

`Trainfree.Admin` can only be used by users in the Administrator role. Non-administrators
(or unprovisioned emails) see a page telling them they don't have access to the app,
instead of the normal app shell.

## Requirements

- `Trainfree.Admin` determines whether to show the app or the "no access" page by calling
  `GET /api/me` at startup. This mirrors the existing `VersionStamp` startup-check
  pattern.
- It renders normally only on `200` with `role == "Administrator"`.
- A `403` shows the "no access" page.
- A network failure or `5xx` shows a distinct "something went wrong, retry" state, rather
  than implying the user lacks permission.
