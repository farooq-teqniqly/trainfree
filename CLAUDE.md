# Trainfree conventions

Baseline conventions (imported below) come from farooq-teqniqly/claude-templates;
update the template first, then re-sync the copy.

@CLAUDE-baseline.md
@CLAUDE-domain-driven-design.md

## What this is

Trainfree -- a self-hosted, single-user workout tracker replacing a paid Trainwell
subscription. Blazor WebAssembly (.NET 10) client, deployed as static assets +
Cloudflare Worker API to Cloudflare Workers, gated by Cloudflare Access. See
`docs/trainfree-proposal.md` for the full one-pager, `docs/screen-mockups.md` for UI, and
`docs/trainfree-roadmap.md` for the slice-by-slice build plan (source of truth for what to
build next and in what order).

## Project-specific rules

- **Two stacks, one Worker.** `src/Trainfree.Web` (Blazor WASM, .NET) is the only project
  in `Trainfree.slnx` -- the `.NET everywhere` convention in the baseline applies to the
  client only. `src/Trainfree.Api` is a single Cloudflare Worker, vanilla JavaScript (no
  TypeScript), sibling folder under `src/`, deliberately outside the solution. One Worker
  deployment serves both: `[assets]` binding serves the Blazor static output, `main`
  handles `/api/*` routes -- single origin, no CORS, one Cloudflare Access policy covers
  the whole app.
- **Prod API URL is never configured.** Same-origin design means the Blazor client's API
  base address is the relative path `/api` in production -- no environment-specific URL,
  no secret. Only `appsettings.Development.json` sets an absolute override
  (`http://127.0.0.1:9999/api/`) for local dev. Port 9999, not wrangler's stock 8787 --
  8787 has been observed to leak orphaned listener processes on Windows across restarts,
  silently hanging every future connection to it until reboot. `wrangler.jsonc`'s `dev.port`
  and `src/Trainfree.Api`'s `predev` npm script (`scripts/Kill-Port.ps1`) both pin to 9999.
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
- **Online-only for v0.1.** No offline queuing or local-first sync for set logging/history
  writes; explicitly deferred to a future version (see roadmap's "Open items").
- **Cloudflare Access is configured manually** in the dashboard (owner's email only) --
  not represented as code in this repo unless that decision changes.