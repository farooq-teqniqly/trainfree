# Slice 1/2 rollout runbook

`IdentityApi` (slice 1) and `AdminApi`'s enforcement of it (slice 2, not yet built) must
deploy in this exact order. Deploying slice 2 before any D1 identity exists locks out
every administrator, including the operator, with no in-app way to recover -- there is no
provisioning UI in this change (see
`docs/identity/identity-intent-01-identityapi.md`'s "Rollout order" requirement, which
this runbook implements). Do not skip step 3.

## Steps

1. **Deploy slice 1.** Push a `v0.0.N` tag (or run `deploy.yaml` manually via
   `workflow_dispatch`) with `IdentityApi` live. Merging this change's PR to `main` alone
   does **not** deploy anything -- `deploy.yaml` only triggers on a `v*.*.*` tag push or a
   manual dispatch. `AdminApi` does not call `IdentityApi` yet, so there is nothing to
   lock out at this point.
2. **Provision at least one `Administrator` identity** against the deployed D1 database:

   ```sh
   cd src/Trainfree.IdentityApi
   npm run provision -- --email <owner's email> --role Administrator --remote
   ```

   Note the email it prints -- step 3 asserts the smoke check's response names this exact
   identity.
3. **Run the rollout smoke check** and confirm it passes before proceeding. This is the
   only step that actually exercises the real service-binding path `AdminApi` will use in
   production; see [Why the smoke check needs its own config](#why-the-smoke-check-needs-its-own-config)
   below for why it cannot be a plain `curl`/browser request.

   ```sh
   cd src/Trainfree.IdentityApi
   export SMOKE_CHECK_INTERNAL_KEY=<ADMIN_INTERNAL_KEY's deployed value>
   export SMOKE_CHECK_JWT=<CF_Authorization cookie value from your own logged-in Trainfree.Admin browser session>
   npm run smoke-check -- --expect-email <the email step 2 provisioned>
   unset SMOKE_CHECK_INTERNAL_KEY SMOKE_CHECK_JWT
   ```

   The internal key and JWT are read from `SMOKE_CHECK_INTERNAL_KEY`/`SMOKE_CHECK_JWT`
   environment variables, not CLI flags, so they never land in shell history or a
   process listing; `unset` them once the check completes. This uses the dedicated
   config at `src/Trainfree.IdentityApi/smoke-harness/wrangler.jsonc`
   (script: `src/Trainfree.IdentityApi/smoke-harness/check.js`), which sends:

   | Header/value | Content |
   | --- | --- |
   | `X-Trainfree-Caller` | `admin` |
   | `X-Trainfree-Internal-Key` | `SMOKE_CHECK_INTERNAL_KEY` (the deployed `ADMIN_INTERNAL_KEY` secret's value) |
   | `Cookie` | `CF_Authorization=<SMOKE_CHECK_JWT>` |

   to `GET /internal/identity` over the real `IDENTITY` service binding to the deployed
   `trainfree-identity-api` Worker. Passing means the script printed `Smoke check PASSED`
   with the email from `--expect-email`; anything else (including a `403`, which means
   "authenticated but unprovisioned") means step 2 or the deployed secrets need fixing
   before continuing -- **do not deploy slice 2 yet.**
4. **Deploy slice 2** (once it exists) with `AdminApi` enforcement enabled.

## Why the smoke check needs its own config

`GET /api/me` -- slice 2's own admin-facing endpoint -- can't be the check, since slice 2
is exactly what step 3 gates. The check has to call `IdentityApi` directly. A plain
`curl`/browser request to `IdentityApi`'s public hostname won't work either: that
hostname is gated by `IdentityApi`'s own, separately-configured Access application (see
`README.md`'s "Cloudflare Access application" section), whose edge policy the
administrator isn't necessarily whitelisted into, and even if they were, the JWT that
edge issues carries `IdentityApi`'s own audience, which fails the caller-specific
audience check regardless.

The check must instead go through the service binding -- the same path `AdminApi` uses
in production -- bypassing the public edge entirely. `smoke-harness/wrangler.jsonc` is a
dedicated Wrangler config for exactly this, kept separate from `AdminApi`'s own local
`wrangler.jsonc` on purpose: `AdminApi`'s local config will carry `LOCAL_DEV_BYPASS` once
slice 2 exists, which synthesizes a local `Administrator` identity and never actually
invokes the binding -- reusing that config would let the check pass even if the real
binding or internal key is broken. `smoke-harness/wrangler.jsonc` has no such var, so
`check.js`'s call always exercises the real binding, and the script asserts the response
names the exact identity step 2 provisioned rather than accepting a bare `200`.
