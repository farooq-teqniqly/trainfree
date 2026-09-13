# Trainfree Identity -- Slice 2: AdminApi enforcement

See [identity-intent.md](identity-intent.md) for shared context and
[identity-intent-01-identityapi.md](identity-intent-01-identityapi.md) for the
`IdentityApi` Worker this slice depends on and calls.

## Scope

Wire every `Trainfree.AdminApi` endpoint to call `Trainfree.IdentityApi` over a
[Cloudflare service binding](https://developers.cloudflare.com/workers/runtime-apis/bindings/service-bindings/)
(Worker-to-Worker, not a public URL) before carrying out its own operation. Add a new
`GET /api/me` endpoint. No `Trainfree.Admin` UI changes in this slice -- that's slice 3.

The browser only ever calls `Trainfree.AdminApi`, same-origin, at the relative `/api` path
(per the existing "prod API URL is never configured, per app" convention -- unchanged by
this design). `AdminApi` serves `GET /api/me` itself and, internally, calls `IdentityApi`
to verify the request's JWT and look up the caller's role, then proxies the result back.

## Requirements

- Every `Trainfree.AdminApi` data endpoint requires the Administrator role, enforced via
  its service-binding call to `IdentityApi`, always sending `X-Trainfree-Caller: admin`
  (see slice 1's contract) -- so a valid `Trainfree.Workout` JWT for an Administrator
  cannot pass this check via a shared audience allowlist; `IdentityApi` validates the
  JWT's `aud` specifically against `Trainfree.Admin`'s Access application because that's
  the caller `AdminApi` names. "Every" is over every route that reads or writes program
  data, not just the CRUD routes -- explicitly including
  `GET /api/programs-tree` and the nested collection/resource GETs
  (`src/Trainfree.AdminApi/src/index.js:597-599`), which are non-CRUD reads and would
  otherwise let an authenticated non-Administrator read program data. `GET /api/version`
  and `OPTIONS` (below) are the only exceptions. A `401` from `IdentityApi` (request
  could not be authenticated at all) or a `403` (authenticated but unprovisioned email
  or unresolvable role) is relayed verbatim, and the underlying operation is not carried
  out. `IdentityApi`'s `200` response does **not** by itself mean "let this through" for
  these endpoints -- per slice 1's contract, `IdentityApi` returns `200` for *any*
  provisioned identity, `Administrator` or `User` alike; it only returns `403` for an
  identity it can't resolve to a role at all, never specifically for "resolved, but
  wrong role." So for every endpoint except `GET /api/me`, `AdminApi` must inspect the
  `200` response body itself and return its own `403` (without calling the underlying
  handler) when `role !== "Administrator"`. Relaying only `IdentityApi`'s own `401`/`403`
  verbatim -- without this inspection step -- would let a provisioned `User` through to
  every Administrator-only endpoint, since `IdentityApi` never gives `AdminApi` a `403`
  for that case.
- `GET /api/me` is the one exception to the Administrator-only rule: it returns
  `200 { "email": string, "role": "Administrator" | "User" }` for *any* provisioned
  identity, Administrator or User, by relaying `IdentityApi`'s `200` response as-is. It
  relays `IdentityApi`'s `401`/`403` the same as every other endpoint -- those still mean
  "couldn't authenticate" / "no matching identity" respectively, not "wrong role."
- `IdentityApi`'s `503` (infrastructure failure -- see slice 1) is relayed as `503` by
  `AdminApi`, not folded into `403`: an outage must stay distinguishable from a denied
  role so slice 3 can show its "something went wrong, retry" state rather than "no
  access." A service-binding call that itself throws or times out (rather than
  returning a `503` cleanly) is likewise surfaced as `503`. In every one of these cases
  -- `401`, `403`, or `503` -- `AdminApi` still fails closed: it never carries out the
  underlying operation unless `IdentityApi` positively returned `200`.
- `GET /api/version` is exempt from this enforcement entirely, regardless of caller.
  It's called two ways: same-origin from the browser (`Trainfree.Versioning`'s
  `VersionCheck`, `src/Trainfree.Versioning/VersionCheck.cs:36-43`), which *does* carry
  the `CF_Authorization` cookie like any other same-origin request, and from
  `deploy.yaml`'s post-deploy verification step (`verify-deployed-version.sh`), which
  authenticates with `CF-Access-Client-Id`/`CF-Access-Client-Secret` service-token
  headers instead -- a CI credential distinct from a user JWT, and the reason this route
  can't just rely on the enforcement wrapper rejecting a missing cookie: the CI caller
  has no cookie to check in the first place, so it would always fail. Routing *either*
  caller through `IdentityApi`'s per-user JWT check would make the deploy-verification
  step fail; the version endpoint is therefore excluded from enforcement outright rather
  than relying on the JWT check to pass or fail correctly for it. `OPTIONS` preflight
  requests are exempt for the
  same reason (no JWT to check).
- An administrator can view all Phases, Exercises, and Programs (i.e. once enforcement is
  in place, existing endpoints keep working end-to-end for an Administrator-role caller).
