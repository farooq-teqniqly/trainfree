# Admin access gating: manual verification

Records how the `admin-access-gating` change's manual verification was carried out, so
it can be re-run the same way later without re-deriving the setup. Covers the two
scenarios that automated bUnit tests can't reach on their own: a real `Trainfree.Admin`
build talking to a real, locally running `AdminApi` Worker (and, for the second
scenario, a real `IdentityApi` Worker).

See `identity-intent-03-admin-access-gating.md` for the frozen intent this verifies, and
`openspec/specs/admin-access-gating/spec.md` for the requirements -- the change that
introduced this behavior has since been closed, per this repo's `trainfree-lean` schema
convention, and its planning artifacts (including the task this doc originally cited)
no longer exist; the shipped requirements live in that main spec instead.

## What was verified

1. **Administrator happy path** -- with `wrangler.jsonc`'s default `LOCAL_DEV_BYPASS`,
   the app renders normally (nav menu, Home page, version indicator) instead of any
   gating page.
2. **Missing-bypass `NoAccess` path** -- with `LOCAL_DEV_BYPASS` off and a real
   `IdentityApi` in front, a browser with no Cloudflare Access session (the only state
   reachable locally, since Access itself doesn't run outside a deployed environment)
   gets a real `401` from `GET /api/me`, and the app renders the `NoAccessPage` instead
   of the app shell.

The `AccessCheckFailed` and `ReauthenticationRequired` outcomes are not covered here --
they're exercised by `AccessCheckTests`/`AccessGateTests`/`AppTests` in
`Trainfree.Admin.Tests` (fake `HttpMessageHandler`/`IAccessCheck`), which is a more
direct way to hit a `5xx`/transport failure or a non-JSON response than reproducing them
against real Workers.

## 1. Administrator happy path

Baseline setup only -- everyday steps from README.md's "Local development" section:

```sh
cd src/Trainfree.AdminApi
npm run db:migrate:local        # only if migrations are behind

cd ../Trainfree.IdentityApi
npm run provision -- --email local-dev@trainfree.local --role Administrator   # idempotent

cd ../Trainfree.AdminApi
npm run dev                     # wrangler dev, LOCAL_DEV_BYPASS=true from wrangler.jsonc
```

In a second terminal, from the repo root:

```sh
dotnet run --project src/Trainfree.Admin/Trainfree.Admin.csproj --launch-profile http
```

Confirmed the identity endpoint resolves the synthetic Administrator before touching a
browser:

```sh
curl -s http://127.0.0.1:9999/api/me
# {"email":"local-dev@trainfree.local","role":"Administrator"}
```

Opened `http://localhost:5280/` in a browser. Result: the Home page rendered inside
`MainLayout` -- nav menu (Programs/Phases/Exercises), the "Welcome back" heading, and the
version-stamp indicator all visible. No gating page shown, confirming the
`Administrator` outcome renders the app normally (spec's "Administrator role renders the
app normally").

## 2. Missing-bypass `NoAccess` path

This scenario needs `AdminApi` to make its real `IDENTITY` service-binding call instead
of taking the `LOCAL_DEV_BYPASS` shortcut, and a real `IdentityApi` on the other end of
that binding -- see README.md's "4. IdentityApi (optional)" section for the normal
version of this setup. `.dev.vars` is gitignored, so these were created for the
verification and deleted afterward; they are not required for everyday dev.

`src/Trainfree.AdminApi/.dev.vars` (temporary):

```
ADMIN_INTERNAL_KEY=local-dev-verification-key
LOCAL_DEV_BYPASS=false
```

`.dev.vars` overrides `wrangler.jsonc`'s `vars` for local `wrangler dev`, so this flips
`LOCAL_DEV_BYPASS` off without editing the checked-in config.

`src/Trainfree.IdentityApi/.dev.vars` (temporary, same key value):

```
ADMIN_INTERNAL_KEY=local-dev-verification-key
WORKOUT_INTERNAL_KEY=unused-for-this-verification
```

Started `IdentityApi` first, then restarted `AdminApi` so it would pick up the new
`.dev.vars` and discover the `IDENTITY` service binding:

```sh
cd src/Trainfree.IdentityApi
npm run dev                     # wrangler dev on 9998

# separate terminal
cd src/Trainfree.AdminApi
npm run dev                     # restart, now with LOCAL_DEV_BYPASS=false
```

Confirmed `IdentityApi` itself was up:

```sh
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:9998/internal/identity
# 401 (missing X-Trainfree-Caller -- expected without AdminApi's headers)
```

Confirmed `AdminApi` now fails closed with a real `401` instead of resolving the
synthetic identity -- no `Cf-Access-Jwt-Assertion`/`CF_Authorization` cookie exists
locally (Cloudflare Access doesn't run in front of `wrangler dev`), so
`extractIdentity` in `Trainfree.IdentityApi/src/identity/providers/cloudflare-access.js`
throws a `JwtVerificationError` before any JWKS fetch, which `handleInternalIdentity`
turns into IdentityApi's own `401`, which `AdminApi`'s `callIdentityApi` relays verbatim:

```sh
curl -s -i http://127.0.0.1:9999/api/me
# HTTP/1.1 401 Unauthorized
# Content-Length: 0
```

Reloaded `http://localhost:5280/` (the Blazor dev server from part 1 was left running).
Result: the page showed only "No access" / "You don't have access to this app." -- no
nav menu, no version indicator, no routed page underneath. Confirms the spec's
"Unauthenticated caller sees 'no access'" scenario and, at the `AccessGate` level, that
`MainLayout` never rendered for this outcome.

## Cleanup

```sh
rm src/Trainfree.AdminApi/.dev.vars
rm src/Trainfree.IdentityApi/.dev.vars
```

Stopped all three `npm run dev`/`dotnet run` processes. `git status` afterward showed no
stray files (`.dev.vars` is gitignored either way, so this step only matters for leaving
the two Workers back on their everyday `LOCAL_DEV_BYPASS=true` behavior next time
`npm run dev` runs).
