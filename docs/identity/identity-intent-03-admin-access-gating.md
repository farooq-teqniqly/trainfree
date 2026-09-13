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
- A `401` or `403` **JSON** response shows the "no access" page -- both mean "you don't
  get in," whether because the request couldn't be authenticated at all or because it
  authenticated to a non-Administrator/unprovisioned identity.
- A network failure or `5xx` shows a distinct "something went wrong, retry" state, rather
  than implying the user lacks permission.
- An **expired Access session is not covered by either state above**, and this doc's
  earlier claim that "Cloudflare Access...already handles re-authentication for an
  expired session, no Trainfree-side handling is needed" is wrong for this specific
  call: that redirect-to-login behavior is what happens on a top-level browser
  navigation, but `GET /api/me` is a `fetch`-style same-origin call from `HttpClient`,
  not a navigation -- a `fetch` redirect doesn't navigate the top-level page. The
  existing `VersionCheck` path already documents the actual observed behavior for this
  exact situation: Cloudflare Access answers the request with its own HTML login page
  (still same-origin, still whatever status Access's edge returns), which is not the
  JSON this slice expects, and parsing it throws `JsonException`
  (`src/Trainfree.Versioning/VersionCheck.cs:58-70`). So a `200`-or-error status check
  alone doesn't cover this case -- the JSON parse itself can fail on a payload that is
  neither a valid "authorized" nor a valid `401`/`403` body. This slice's `/api/me` call
  must catch that non-JSON-response case (mirroring `VersionCheck`'s catch clause) as a
  fourth, distinct outcome, and handle it by forcing a **top-level** navigation reload
  (e.g. a full-page `location.reload()`/`NavigationManager.NavigateTo(..., forceLoad:
  true)`, not another `fetch`) -- that reload is a real top-level navigation, which is
  what actually lets Cloudflare Access's edge redirect to its login page and
  re-authenticate the session. Treating this response as "no access" would be
  misleading (the user may well be an Administrator whose session simply expired), and
  treating it as "something went wrong, retry" would loop forever since a plain retry
  is still a `fetch`, not a navigation.
