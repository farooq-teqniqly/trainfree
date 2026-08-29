## Why

Slice 1 (`add-programs-crud`) was built directly inside `src/Trainfree.Web`, the shared
project that predates the admin/workout split described in `trainfree-proposal.md`'s "Two
client apps, one Worker" section. Slices 3-4 continue extending admin CRUD, and slice 5
begins the workout app. Doing the restructure now -- before slices 3-4 add more
admin-only code, and before slice 5 needs its own project and Worker -- means later
slices are built directly in their final home instead of being moved after the fact.

Rather than one Worker serving two SPAs under different paths (the shape originally
sketched in `trainfree-proposal.md`), each app gets its own independent Cloudflare
Worker: its own static assets, its own `/api/*` routes, its own deploy pipeline, bound
to the same physical D1 database. This avoids the ambiguity of a single
`not_found_handling: single-page-application` fallback having to serve two different
SPA shells from one assets binding, and lets the two apps be deployed, scaled, and
iterated on independently.

## What Changes

- Rename `src/Trainfree.Web` -> `src/Trainfree.Admin` (and its test project
  `tests/Trainfree.Web.Tests` -> `tests/Trainfree.Admin.Tests`). No behavior change --
  same routes, same pages, same `Admin/` CRUD code, just relocated and renamed.
- Extract `Ids/` (`DomainId`, `CrockfordBase32`, `ProgramId`) out of `Trainfree.Web` into
  a new plain class library, `Trainfree.Domain`, with no Blazor/UI dependency, plus a
  sibling `Trainfree.Domain.Tests` project. `Trainfree.Admin` references it.
- Extract `Versioning/` (`IVersionCheck`, `VersionCheck`, `VersionCheck.Logging.cs`,
  `VersionStamp`) and `Layout/VersionIndicator.razor` (+ its `.razor.css`) out of
  `Trainfree.Web` into a new Razor Class Library, `Trainfree.Versioning`, plus a sibling
  `Trainfree.Versioning.Tests` project. Logic and its one UI component stay together in
  one project -- they are not independently reusable. `Trainfree.Admin` references it.
- **BREAKING (deploy identity)**: rename `src/Trainfree.Api` -> `src/Trainfree.AdminApi`.
  The deployed Worker's `name` changes from `trainfree` to `trainfree-admin` in
  `wrangler.jsonc` and `wrangler.deploy.jsonc`, which changes its `workers.dev` URL to
  `trainfree-admin.<account-subdomain>.workers.dev`. This proves the deploy pipeline
  (build, migrate, deploy, version-verify) works end to end against a freshly named
  Worker rather than assuming it would.
- Update `.github/workflows/deploy.yaml` and `.github/scripts/verify-deployed-version.sh`
  paths/references to match the renamed projects (`Trainfree.AdminApi`,
  `Trainfree.Admin`).
- Update `Trainfree.slnx` to register the renamed and new projects.
- Rewrite CLAUDE.md's "Project-specific rules" section to describe the target two-Worker
  architecture (one Worker per app, each with its own D1 binding to the same database,
  no combined asset directory, no shared SPA fallback) instead of the single-Worker
  design it currently documents. The workout half of this architecture (`Trainfree.Workout`,
  `Trainfree.WorkoutApi`) is described but not built until slice 5 -- this change updates
  the doc to match the now-intended shape without building the second half prematurely.
- No new `src/Trainfree.Workout` or `src/Trainfree.WorkoutApi` project is created in this
  change. Unlike the original roadmap sketch (an empty stub app created now), the workout
  app is built for real in slice 5, once there is actual work to put in it.
- Out of repo scope, called out for the user to do manually alongside this change:
  reconfiguring the Cloudflare Access application/policy for the Worker's new hostname
  (Access is dashboard-only per CLAUDE.md, not represented as code in this repo).

## Capabilities

### New Capabilities
- `admin-worker-deployment`: the observable, testable contract of the renamed Admin
  Worker's deployment identity -- its name/origin, its `/api/version` stamp endpoint,
  and its D1 database binding. Captures the same kind of deploy-time contract CLAUDE.md
  already treats as load-bearing (the two-stamp/version-verify mechanism), now specific
  to the Admin Worker by name.

### Modified Capabilities
(none -- the `programs` spec's requirements, API contract, and behavior are unchanged;
only the project layout and deployed Worker identity change)

## Impact

- **Affected projects**: `src/Trainfree.Web` (renamed to `src/Trainfree.Admin`),
  `src/Trainfree.Api` (renamed to `src/Trainfree.AdminApi`),
  `tests/Trainfree.Web.Tests` (renamed to `tests/Trainfree.Admin.Tests`). New projects:
  `src/Trainfree.Domain`, `tests/Trainfree.Domain.Tests`, `src/Trainfree.Versioning`,
  `tests/Trainfree.Versioning.Tests`.
- **Affected config**: `Trainfree.slnx`, `wrangler.jsonc`, `wrangler.deploy.jsonc`,
  `.github/workflows/deploy.yaml`, `.github/scripts/verify-deployed-version.sh`.
- **Affected docs**: `CLAUDE.md` (Project-specific rules), `docs/trainfree-roadmap.md`
  (slice 2 description, once implemented).
- **Deploy identity change**: the production Worker's name and URL change
  (`trainfree` -> `trainfree-admin`). Cloudflare Access must be manually repointed at
  the new hostname before the next deploy is reachable through Access -- an operational
  step outside this repo's code.
- **No API or D1 schema changes**: `programs` table, routes, and request/response
  contracts are untouched.
