## 1. Rollout verification

- [ ] 1.1 Deploy `IdentityApi` to a real Cloudflare environment from `main` (per
      `deploy.yaml`'s `deploy-identity-api` job) with no callers wired up yet. Verify:
      the deploy job succeeds and `GET /api/version` reports the deployed stamp.
- [ ] 1.2 Run the provisioning script against the deployed D1 database once, per
      `docs/identity/rollout-runbook.md` step 2 (`npm run provision -- --email <owner's
      email> --role Administrator --remote`). Verify: the script reports success and
      prints the provisioned email.
- [ ] 1.3 Run the rollout smoke check via the dedicated smoke-harness config, per
      `docs/identity/rollout-runbook.md` step 3, and confirm `/internal/identity`
      resolves that identity to `200`/`Administrator` -- the exact identity 1.2
      provisioned, not a synthetic value. Verify: the smoke-harness script's output
      shows "Smoke check PASSED" naming the real `email`/`userId` from 1.2.
