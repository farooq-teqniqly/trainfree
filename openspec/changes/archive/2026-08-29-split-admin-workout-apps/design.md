## Context

`src/Trainfree.Web` (Blazor WASM) and `src/Trainfree.Api` (Cloudflare Worker, vanilla
JS) currently carry both the admin CRUD built in slice 1 and the general-purpose plumbing
(domain IDs, deploy-stamp version checking) that the not-yet-built workout app will also
need starting at slice 5. `docs/trainfree-proposal.md`'s "Two client apps, one Worker"
section originally sketched a single Worker serving both apps' static assets from one
combined directory (`Trainfree.Workout` at `/`, `Trainfree.Admin` at `/admin`), with one
`not_found_handling: single-page-application` fallback. That shape was rejected during
exploration: a single SPA-fallback document can't unambiguously serve two different SPA
shells depending on which path segment failed to resolve, and the two apps have no
actual coupling that requires them to share a Worker or an origin.

## Goals / Non-Goals

**Goals:**
- Rename and relocate today's admin app/worker into their final two-Worker-architecture
  homes with zero behavior change to the `programs` capability.
- Extract the code that both apps will eventually need (`Ids/`, `Versioning/`) into
  shared libraries now, so slice 5 references them instead of duplicating and later
  de-duplicating.
- Prove the renamed Worker's deploy pipeline (build, D1 migrate, deploy, version-verify)
  works against a fresh Worker name/URL.
- Bring CLAUDE.md's documented architecture in line with the two-Worker decision.

**Non-Goals:**
- Building `Trainfree.Workout` or `Trainfree.WorkoutApi` -- that's slice 5, a separate
  change, built for real rather than as a stub moved into today.
- Any change to the `programs` capability's API contract, D1 schema, or UI behavior.
- Creating a `Trainfree.UI` shared component library -- nothing exists yet that
  qualifies as an audience-agnostic shared component; `VersionIndicator` stays inside
  `Trainfree.Versioning` since it's not independently reusable from the version-check
  logic it renders.
- Reconfiguring Cloudflare Access -- manual/dashboard-only per CLAUDE.md, out of repo
  scope, called out as an operational follow-up for the user.

## Decisions

### One Worker per app, not one Worker serving two SPAs

Each app (`Trainfree.Admin` now, `Trainfree.Workout` in slice 5) gets its own Cloudflare
Worker: own `[assets]` binding serving only that app's `wwwroot`, own `main` handling
only that app's `/api/*` routes, own `wrangler.jsonc`/`wrangler.deploy.jsonc` pair, own
deploy job. Both Workers bind the same physical D1 database (`trainfree_db`, same
`database_id`) -- D1 supports multiple Workers binding one database, and there is one
logical dataset regardless of how many Workers read/write it.

**Alternative considered**: one Worker, combined assets directory, path-prefixed SPA
fallback logic implemented by hand in `index.js` (checking the request path against
`/admin/*` vs everything else before calling `env.ASSETS.fetch()`). Rejected: it
reintroduces the fallback-ambiguity problem as custom code to maintain instead of
removing it, and gains nothing over two independent Workers since the apps share no
runtime state beyond the D1 database, which two Workers can already both bind.

### Renaming `Trainfree.Api` -> `Trainfree.AdminApi` now, including the deployed Worker name

`wrangler.jsonc`/`wrangler.deploy.jsonc`'s `name` changes from `trainfree` to
`trainfree-admin`, changing the Worker's `workers.dev` URL. Done now, in this change,
rather than deferred to slice 5, specifically so the rename is proven by the CI deploy
pipeline (`deploy.yaml`'s publish -> migrate -> deploy -> verify-deployed-version
sequence) actually succeeding against the new name, rather than assumed safe.

**Alternative considered**: keep the Worker named `trainfree` until slice 5 introduces
`trainfree-workout`, avoiding a URL change with no immediately visible payoff. Rejected
per explicit direction: verifying the rename against the live deploy pipeline now is
more valuable than deferring it, and deferring would mean the *second* rename (slice 5)
is the first time the pipeline is exercised under a new name, with no baseline from a
first rename to compare against if it breaks.

### Two shared libraries (`Trainfree.Domain`, `Trainfree.Versioning`), not three

- `Trainfree.Domain`: plain class library (`Microsoft.NET.Sdk`), no Blazor/UI reference.
  Carries `DomainId`, `CrockfordBase32`, `ProgramId`. Framework-free by DDD convention,
  so it's referenceable from any future non-Blazor context too, not just both Blazor
  apps.
- `Trainfree.Versioning`: Razor Class Library (`Microsoft.NET.Sdk.Razor`). Carries
  `IVersionCheck`, `VersionCheck`, `VersionCheck.Logging.cs`, `VersionStamp`, and the one
  Razor component that renders their outcome, `VersionIndicator.razor` (+ `.razor.css`).
  Logic and its one UI consumer stay in the same project because they are not
  independently useful apart -- splitting them into a UI-only project and a logic-only
  project would be paying for layering with no reuse benefit, since nothing else
  consumes either half separately.

**Alternative considered**: a third `Trainfree.UI` project to hold `VersionIndicator`
separately from version-check logic, anticipating that future shared components land
there too. Rejected as premature: there is exactly one shared component today, it has an
obvious home already, and creating an empty-ish placeholder project ahead of a second
real component violates the "don't design for hypothetical future requirements"
convention. Create `Trainfree.UI` in whichever slice first needs a second genuinely
audience-agnostic component.

### CLAUDE.md rewrite scope

CLAUDE.md's "Project-specific rules" section (the "Two stacks, one Worker" bullet and
the "Prod API URL is never configured" same-origin rationale) currently documents the
single-Worker design as settled fact. This change rewrites that section to describe the
two-Worker-per-app architecture as the durable rule, including the not-yet-built
`Trainfree.Workout`/`Trainfree.WorkoutApi` half, so the doc matches the now-intended
shape rather than lagging behind it until slice 5 lands. The "Prod API URL is never
configured" rule (relative `/api` base address, no environment-specific URL) still holds
per-Worker -- each app's Worker serves its own assets and its own `/api/*` at the same
origin, it's just that "the app" now means one specific Worker instead of the shared one.

## Risks / Trade-offs

- **[Risk]** Renaming the deployed Worker changes its `workers.dev` URL and the hostname
  Cloudflare Access is scoped to; a deploy could succeed while the site is unreachable
  through Access until the dashboard config is updated. -> **Mitigation**: called out
  explicitly in the proposal and `tasks.md` as a manual step to do alongside this
  change, not something the deploy pipeline can verify or automate.
- **[Risk]** `verify-deployed-version.sh`'s `BASE_URL` falls back to
  `steps.deploy.outputs.deployment-url` when `APP_BASE_URL` isn't set -- if the repo
  variable still points at the old `trainfree.*.workers.dev` hostname, the verify step
  could pass against a stale URL while the real rename goes unverified, or fail
  confusingly. -> **Mitigation**: confirm/update the `APP_BASE_URL` repository variable
  as part of this change's manual steps, before relying on the verify step's result.
- **[Trade-off]** Two Workers means two `wrangler.jsonc`/`wrangler.deploy.jsonc` pairs
  and two deploy jobs to keep in sync (compatibility_date, D1 binding) once slice 5
  lands, versus one pair today. Accepted: the alternative (one Worker, hand-rolled
  path-based SPA fallback) was rejected above for carrying its own, worse-hidden
  maintenance cost.

## Migration Plan

1. Create `Trainfree.Domain` and `Trainfree.Versioning` (+ test projects), move the
   relevant files out of `Trainfree.Web` into them, update references.
2. Rename `Trainfree.Web` -> `Trainfree.Admin` and its test project; update `Trainfree.slnx`.
3. Rename `Trainfree.Api` -> `Trainfree.AdminApi`; update `wrangler.jsonc`/
   `wrangler.deploy.jsonc` `name` to `trainfree-admin` and the `assets.directory` path to
   match the renamed publish output.
4. Update `deploy.yaml` and `verify-deployed-version.sh` for the renamed paths/URL.
5. Rewrite CLAUDE.md's Project-specific rules.
6. Manually (outside this repo's code, by the user): repoint Cloudflare Access at the
   new hostname, confirm/update `APP_BASE_URL`.
7. Tag and deploy (`v0.0.N`) to exercise the renamed pipeline end to end; confirm
   `verify-deployed-version.sh` passes against the live `trainfree-admin` Worker.

No rollback beyond reverting the commit/tag and redeploying the previous tag -- there is
no data migration (D1 schema untouched) and no user-facing downtime risk beyond the
deploy window itself.

## Open Questions

- None outstanding -- all decisions above were confirmed during exploration.
