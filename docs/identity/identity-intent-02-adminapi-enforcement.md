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

- All `Trainfree.AdminApi` endpoints only allow authorized requests -- Administrator role
  required for every endpoint in this Worker, enforced via its service-binding call to
  `IdentityApi`. A `403` from `IdentityApi` (unprovisioned email or non-Administrator
  role) is relayed verbatim.
- Any response from `IdentityApi` other than a clean `200` or `403` -- a service-binding
  call that throws, times out, or returns any other status -- is treated as `403` by
  `AdminApi`. `AdminApi` fails closed: it never carries out the underlying operation
  when it cannot positively confirm authorization, even if the failure is on
  `IdentityApi`'s side rather than the caller's.
- An administrator can view all Phases, Exercises, and Programs (i.e. once enforcement is
  in place, existing endpoints keep working end-to-end for an Administrator-role caller).
- `GET /api/me` returns `200 { "email": string, "role": "Administrator" | "User" }` on
  success by relaying `IdentityApi`'s response, or the `403` it returns.
