## 1. Real Access configuration

- [ ] 1.1 Look up (Zero Trust dashboard or Cloudflare API) the account's Access team
      domain and `Trainfree.Admin`'s existing Cloudflare Access application's audience
      tag -- `Trainfree.Admin` already has its own Access application from an earlier
      slice (see CLAUDE.md's "Cloudflare Access applications are provisioned via the
      Cloudflare API" rule: one application per app, reusing the same two policies by
      ID). This is not a new application to create; `WORKOUT_AUDIENCE` stays a
      placeholder until `Trainfree.Workout` gets its own application in a later slice.
      Verify: the team domain and `Trainfree.Admin` audience values are in hand,
      recorded nowhere but this task's PR/commit (they are non-secret but
      account-specific).
- [ ] 1.2 Replace `ACCESS_TEAM_DOMAIN`/`ADMIN_AUDIENCE`'s `REPLACE_WITH_...`
      placeholders in `src/Trainfree.IdentityApi/wrangler.jsonc` with the real values
      from 1.1, leaving `WORKOUT_AUDIENCE` as-is. Verify: `deploy.yaml`'s "Verify no
      placeholder Access config values remain" guard step would now pass locally
      (`grep -E '"(ACCESS_TEAM_DOMAIN|ADMIN_AUDIENCE)":\s*"REPLACE_WITH_'
      src/Trainfree.IdentityApi/wrangler.jsonc` finds nothing).

## 2. Rollout verification

- [ ] 2.1 Deploy `IdentityApi` to the real Cloudflare environment from `main` (per
      `deploy.yaml`'s `deploy-identity-api` job) with no callers wired up yet. Verify:
      the deploy job succeeds and `GET /api/version` reports the deployed stamp.
- [ ] 2.2 Before relying on it, confirm `scripts/provision-identity.js --remote`
      actually reaches the deployed D1 database rather than silently operating on
      local state. `getPlatformProxy`'s `remoteBindings` option only routes a binding
      remotely if that binding's own config also declares `"remote": true` --
      `wrangler.jsonc`'s `d1_databases` entry deliberately doesn't (it would make
      plain local dev/`npm run dev` hit production D1 by default). If `--remote`
      doesn't reach production, add a dedicated config for remote provisioning with
      `"remote": true` on the `DB` binding (mirroring
      `smoke-harness/wrangler.jsonc`'s separate-config pattern for the same reason),
      and point `provision-identity.js --remote` at it instead of the shared
      `wrangler.jsonc`. Verify: a row inserted via `--remote` is visible via
      `wrangler d1 execute trainfree_db --remote --command "SELECT * FROM logins"`,
      not just in the local `.wrangler-shared` SQLite file.
- [ ] 2.3 Run the provisioning script against the deployed D1 database once (now
      confirmed reaching it per 2.2), per `docs/identity/rollout-runbook.md` step 2
      (`npm run provision -- --email <owner's email> --role Administrator --remote`).
      Verify: the script reports success and prints the provisioned email.
- [ ] 2.4 Before relying on it, confirm `smoke-harness/check.js` actually reaches the
      deployed `IdentityApi` Worker over the real service binding rather than failing
      or no-op'ing locally -- same `remote: true`-per-binding caveat as 2.2 applies to
      `wrangler.jsonc`'s `services` entry (`IDENTITY`) in `smoke-harness/wrangler.jsonc`.
      Add `"remote": true` to that binding if the check does not actually reach
      production. Verify: `npm run smoke-check` genuinely fails (not just returns a
      generic error) when pointed at a caller with a deliberately wrong internal key,
      proving it is talking to the real deployed Worker's key-check logic.
- [ ] 2.5 Run the rollout smoke check via the dedicated smoke-harness config, per
      `docs/identity/rollout-runbook.md` step 3, and confirm `/internal/identity`
      resolves that identity to `200`/`Administrator` -- the exact identity 2.3
      provisioned, not a synthetic value. Verify: the smoke-harness script's output
      shows "Smoke check PASSED" naming the real `email`/`userId` from 2.3.
