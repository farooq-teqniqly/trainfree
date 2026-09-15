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
- [ ] 2.2 Run the provisioning script against the deployed D1 database once, per
      `docs/identity/rollout-runbook.md` step 2 (`npm run provision -- --email <owner's
      email> --role Administrator --remote`). Verify: the script reports success and
      prints the provisioned email.
- [ ] 2.3 Run the rollout smoke check via the dedicated smoke-harness config, per
      `docs/identity/rollout-runbook.md` step 3, and confirm `/internal/identity`
      resolves that identity to `200`/`Administrator` -- the exact identity 2.2
      provisioned, not a synthetic value. Verify: the smoke-harness script's output
      shows "Smoke check PASSED" naming the real `email`/`userId` from 2.2.
