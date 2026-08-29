# Trainfree conventions

Baseline conventions (imported below) come from farooq-teqniqly/claude-templates;
update the template first, then re-sync the copy.

@CLAUDE-baseline.md
@CLAUDE-domain-driven-design.md
@CLAUDE-blazor-ui.md

## What this is

Trainfree -- a self-hosted, single-user workout tracker replacing a paid Trainwell
subscription. Blazor WebAssembly (.NET 10) client, deployed as static assets +
Cloudflare Worker API to Cloudflare Workers, gated by Cloudflare Access. See
`docs/trainfree-proposal.md` for the full one-pager, `docs/screen-mockups.md` for UI,
`docs/trainfree-roadmap.md` for the slice-by-slice build plan (source of truth for what to
build next and in what order), and `docs/ui-decisions.md` for why the front-end
conventions were chosen.

## Project-specific rules

- **Two apps, one Worker each.** The Blazor client is split into two independent Blazor
  WASM projects, one per audience: `src/Trainfree.Admin` (CRUD for programs/sessions/
  exercises, built first) and `src/Trainfree.Workout` (the workout runner, not yet
  built -- see the roadmap's slice 5+). Each app deploys behind its own Cloudflare
  Worker, not a shared one: `src/Trainfree.AdminApi` for `Trainfree.Admin`, and a future
  `src/Trainfree.WorkoutApi` for `Trainfree.Workout` -- vanilla JavaScript (no
  TypeScript), sibling folders under `src/`, deliberately outside the solution. Each
  Worker's `[assets]` binding serves only its own app's static output and its `main`
  handles only its own `/api/*` routes -- no combined assets directory, no path-prefixed
  SPA fallback shared between two apps. Both Workers bind the same physical D1 database
  (`trainfree_db`) -- one logical dataset, two Workers reading/writing it, no data
  duplication or sync step. This was chosen over a single Worker serving both apps'
  assets under different paths (`/` and `/admin`) because one
  `not_found_handling: single-page-application` fallback document cannot unambiguously
  serve two different SPA shells depending on which path segment failed to resolve --
  see the `split-admin-workout-apps` OpenSpec change's `design.md` for the fuller
  rationale.
  `Trainfree.Domain` (domain value objects/IDs, e.g. `ProgramId`) and
  `Trainfree.Versioning` (the deploy-stamp check + its one Razor component) are shared
  class libraries both apps reference, so slice 5 doesn't duplicate what `Trainfree.Admin`
  already built. The `.NET everywhere` convention in the baseline applies to all of these
  client-side projects; the Workers stay JavaScript.
- **Prod API URL is never configured, per app.** Same-origin design means each app's API
  base address is the relative path `/api` in production, resolved against that app's own
  Worker -- no environment-specific URL, no secret. Only each app's
  `appsettings.Development.json` sets an absolute override (`http://127.0.0.1:9999/api/`
  for `Trainfree.Admin` against `Trainfree.AdminApi`) for local dev. Port 9999, not
  wrangler's stock 8787 -- 8787 has been observed to leak orphaned listener processes on
  Windows across restarts, silently hanging every future connection to it until reboot.
  Each Worker's `wrangler.jsonc`'s `dev.port` and `predev` npm script
  (`scripts/Kill-Port.ps1`) pin to 9999; only one Worker runs locally at a time today
  since `Trainfree.WorkoutApi` doesn't exist yet -- when it does, it needs its own,
  different local dev port.
- **Persistence: Cloudflare D1 (SQLite)**, reached only from the Worker via its native D1
  binding -- the Blazor client never talks to D1 directly. Exercise images: Cloudflare R2,
  URL stored on the `Exercise` record in D1.
- **TDD applies to the Worker too.** JS Worker code follows the same red-green-refactor
  discipline as the baseline's .NET TDD rule. Test with `vitest` +
  `@cloudflare/vitest-pool-workers`, plain `.test.js` files, run against the real Workers
  runtime via Miniflare with real D1 bindings -- no mocking layer. `ci.yaml`'s
  `SONAR_EXCLUSIONS` already excludes all `.js` from the Sonar coverage metric, so Worker
  tests are enforced by convention/review, not by the coverage gate.
- **D1 schema migrations: `wrangler d1 migrations`.** Any slice that changes the schema
  adds a migration file. `wrangler d1 migrations apply` runs automatically in
  `deploy.yaml` on every tag. One-time resource creation (`wrangler d1 create`,
  `wrangler r2 bucket create`) is a separate, non-idempotent one-time setup script --
  never part of the deploy pipeline; the resulting `database_id` / bucket name are
  committed into `wrangler.jsonc` (not secrets).
- **Deploy: tag-per-slice, `v0.0.N`.** `deploy.yaml` triggers on `v*.*.*` tags. After
  merging a slice's PR to `main`, push a `v0.0.N` tag to deploy. Bump to `v0.1.0` when the
  first real milestone is judged complete -- not tied to a specific slice.
- **Every deploy is stamped twice, and the two stamps must agree.** `deploy.yaml` computes
  one `<tag>+<short-sha>` stamp and injects it both into the Blazor assembly
  (`-p:InformationalVersion`) and into the Worker (`wrangler deploy --var APP_VERSION/APP_COMMIT`).
  The client compares its compiled-in stamp against `GET /api/version` on startup and shows a
  reload banner when they differ, so a stale bundle can no longer serve silently (issue #18).
  Anything that changes how either side is stamped has to change both, or the banner fires on
  every load. `Trainfree.Admin.csproj` disables `IncludeSourceRevisionInInformationalVersion`
  for exactly that reason. The stamp-reading type (`VersionStamp`, in `Trainfree.Versioning`)
  reads its own compiled assembly's attribute, not the app's -- MSBuild propagates
  `-p:InformationalVersion` as a global property to referenced projects by default, so
  `Trainfree.Versioning.dll` gets stamped too when built as part of `Trainfree.Admin`'s
  publish, just without the source-revision-suffix override, which `VersionStamp`'s
  existing full-SHA-suffix stripping already absorbs. `deploy.yaml`'s final step polls
  the live `GET /api/version` and fails the job unless it reports that same stamp, so a
  Worker deployed without the vars is caught in CI instead of by opening the site. That
  step reaches through Cloudflare Access
  with a service token (`CF_ACCESS_CLIENT_ID` / `CF_ACCESS_CLIENT_SECRET` secrets, a
  different credential from `CLOUDFLARE_API_TOKEN`) against the origin in the `APP_BASE_URL`
  repository variable; the token and its Service Auth policy are created manually in the
  Zero Trust dashboard, like the rest of the Access config.
- **Online-only for v0.1.** No offline queuing or local-first sync for set logging/history
  writes; explicitly deferred to a future version (see roadmap's "Open items").
- **Cloudflare Access is configured manually** in the dashboard (owner's email only) --
  not represented as code in this repo unless that decision changes.
- **Bootstrap and Bootstrap Icons are CDN-pinned with SRI**, no vendored `wwwroot/lib`
  copy -- see `CLAUDE-blazor-ui.md`'s Asset delivery rule for the pattern this follows.
- **Brand values for the Blazor UI conventions:** Inter as the typeface, black
  `.btn-primary` (`#000`, `#1a1a1a` on hover), and the Bootstrap dashboard shell (dark
  sticky navbar, light `bg-body-tertiary` sidebar) -- all overridden in `app.css`.
