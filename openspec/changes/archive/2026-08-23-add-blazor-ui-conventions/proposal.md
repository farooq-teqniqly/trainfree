## Why

Releases v0.0.7 (#31, #32) and the untagged v0.0.8 set (#33, #34, #35) made a series of
front-end decisions that exist only as commit messages and code. Two of them were
corrections to mistakes that written conventions would have prevented: an empty
`<a href="">` brand link that resolved to the current document instead of routing home
(#32), and inline `<svg>` markup duplicating icons the already-loaded Bootstrap Icons font
provides (#33).

The asset-delivery decision is demonstrably cross-repo and already decaying. pr-center's
`App.razor` carries the same CDN + Inter + preconnect block that #31 introduced here --
its commit message says so explicitly ("matching the pattern used in pr-center") -- but
the two have already drifted: pr-center pins Bootstrap 5.3.3 with no SRI hash, trainfree
pins 5.3.8 with one. A pattern that propagates by copy-paste and is written down nowhere
degrades exactly this way.

## What Changes

- Add `CLAUDE-blazor-ui.md` to claude-templates as a new opt-in module (asset delivery,
  icons, navigation/accessibility, testability, editable rows, styling), with an
  `-IncludeBlazorUi` flag in `Initialize-Project.ps1` and a README entry.
- Sync that file into this repo and import it from `CLAUDE.md`.
- Add the repo-specific brand values (Inter, black `.btn-primary`, the Bootstrap dashboard
  shell) to `CLAUDE.md`'s Project-specific rules -- values stay here, rules stay in the
  shared module, so the synced copy remains byte-identical across repos.
- Add `docs/ui-decisions.md` recording why each decision was made, including the two bugs
  that motivated the rules.
- Replace `ProgramRow`'s primary constructor with an explicit one, resolving a conflict
  with the baseline's "No primary constructors" rule.
- Backfill the `programs` spec with the `Revert` affordance, which shipped in #35 as a
  plain `fix` PR without passing through OpenSpec.

## Capabilities

### New Capabilities
(none -- conventions, documentation, and a non-behavioral refactor)

### Modified Capabilities
- `programs`: spec-only backfill. The `Admin program list UI` requirement documents
  `Save` button visibility in three scenarios but never mentions `Revert`, which has been
  live since #35. No code changes -- the spec is being brought up to the shipped
  behavior. The `ProgramRow` refactor is observably equivalent and is already covered by
  the existing `ProgramsPageTests`.

## Impact

- New: `CLAUDE-blazor-ui.md` (repo root), `docs/ui-decisions.md`.
- Modified: `CLAUDE.md` (one import, two bullets),
  `src/Trainfree.Web/Pages/Admin/Programs.razor` (`ProgramRow` constructor only).
- Cross-repo: `claude-templates` needs the module, the `Initialize-Project.ps1` flag, and
  the README row landed **first**, per the "update the template first, then re-sync"
  rule. That is a separate PR in a separate repository; this change depends on it.
- Always-on context grows by roughly 40 lines against a ~420-line base (`CLAUDE.md` plus
  its imports). This is the deliberate cost of the module being always-on rather than a
  skill -- see design.md.
- No deployment required: docs plus a non-behavioral refactor. `ci.yaml` skips docs-only
  PRs, but the `Programs.razor` edit means this PR is not docs-only and will run the full
  build and test suite.
