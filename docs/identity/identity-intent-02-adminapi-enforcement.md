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

- Every `Trainfree.AdminApi` CRUD endpoint (Programs, Exercises, Phases, Sessions, etc.)
  requires the Administrator role, enforced via its service-binding call to
  `IdentityApi`. A `401` from `IdentityApi` (request could not be authenticated at all)
  or a `403` (authenticated but unprovisioned email or non-Administrator role) is
  relayed verbatim, and the underlying operation is not carried out.
- `GET /api/me` is the one exception to the Administrator-only rule: it returns
  `200 { "email": string, "role": "Administrator" | "User" }` for *any* provisioned
  identity, Administrator or User, by relaying `IdentityApi`'s `200` response as-is. It
  relays `IdentityApi`'s `401`/`403` the same as every other endpoint -- those still mean
  "couldn't authenticate" / "no matching identity" respectively, not "wrong role."
- Any response from `IdentityApi` other than a clean `200`, `401`, or `403` -- a
  service-binding call that throws, times out, or returns any other status -- is treated
  as `403` by `AdminApi`. `AdminApi` fails closed: it never carries out the underlying
  operation when it cannot positively confirm authorization, even if the failure is on
  `IdentityApi`'s side rather than the caller's.
- `GET /api/version` is exempt from this enforcement entirely -- it is not called with a
  `CF_Authorization` cookie at all. `deploy.yaml`'s post-deploy verification step
  (`verify-deployed-version.sh`) calls it with `CF-Access-Client-Id`/
  `CF-Access-Client-Secret` service-token headers, a CI credential distinct from a user
  JWT; routing that call through `IdentityApi`'s per-user JWT check would make the
  deploy-verification step itself fail. `OPTIONS` preflight requests are exempt for the
  same reason (no JWT to check).
- An administrator can view all Phases, Exercises, and Programs (i.e. once enforcement is
  in place, existing endpoints keep working end-to-end for an Administrator-role caller).
