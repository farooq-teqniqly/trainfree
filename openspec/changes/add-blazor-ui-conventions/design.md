## Context

Conventions in this repo live in three always-on files imported by `CLAUDE.md`:
`CLAUDE-baseline.md` (synced from claude-templates), `CLAUDE-domain-driven-design.md`
(an opt-in template module), and `CLAUDE.md` itself (project-specific rules). There is no
home for front-end conventions, so the decisions from v0.0.7/v0.0.8 have none.

claude-templates has already faced a version of the "always-on module or on-demand skill"
question and recorded its answer in its README: `review-spec` (skill) and
`CLAUDE-product-manager.md` (`-IncludePm`) are two forms of one behavior, and "the skill
is the default." This change departs from that default, so the reasoning is recorded here
rather than left implicit.

## Goals / Non-Goals

**Goals:**
- Give the v0.0.7/v0.0.8 decisions a written home that survives into future slices.
- Put the portable subset somewhere pr-center and future Blazor repos can consume.
- Resolve the primary-constructor conflict before the pattern it affects is promoted into
  a written convention.

**Non-Goals:**
- Changing any shipped UI behavior. The `ProgramRow` refactor is observably equivalent.
- Fixing pr-center's drift (5.3.3, no SRI). Bumping a working app's Bootstrap version is a
  real change that deserves its own PR and its own revert boundary; burying it in a
  conventions commit makes it un-revertable on its own. Filed as an issue instead.
- Retrofitting `docs/screen-mockups.md` to match what shipped.

## Decisions

- **Always-on module, not a skill.** A skill must decide it is relevant, and these rules
  matter precisely when the model is thinking about something else -- "add a Revert button
  to the programs table" contains no signal that UI conventions apply. That is the exact
  moment #32's empty `href` and #33's inline SVG were written. Both were caught at review,
  not prevented; prevention is the entire point of writing them down. The distinction from
  `review-spec` is that PM review is an *activity the user invokes*, whereas these are
  *ambient constraints on activities undertaken for other reasons*. Skill triggering is
  reliable for the former and unreliable for the latter.
- **Cost is acknowledged, not hidden.** An `@import` is inlined into every session exactly
  as if pasted; it buys sync discipline and readability, not context budget. The module is
  kept to roughly 40 lines for that reason, and anything that only a human needs goes to
  `docs/ui-decisions.md` instead.
- **Rules in the shared module, values in `CLAUDE.md`.** The module contains no `#000`, no
  "Inter", no version pins. This keeps the synced copy byte-identical across consuming
  repos, so re-syncing stays a copy rather than a merge. `CLAUDE-baseline.md` already pays
  for `{{PROJECT_NAME}}` substitution; a second parameterized file would compound that.
- **Named `CLAUDE-blazor-ui.md`, not `CLAUDE-web-ui.md`.** Roughly half the rules are
  plain HTML (CDN pinning, SRI, icon font over inline SVG, ARIA floor) and would serve a
  non-Blazor repo. The other half are Blazor-bound (`NavLink` over `<a href="">`,
  `data-testid` conventions that exist for bUnit, the row view-model shape). Splitting
  yields two ~18-line files and doubles the sync burden for one hypothetical consumer;
  the Blazor-specific half is also the half that caught real bugs.
- **Google Fonts is a documented exception to the SRI rule.** Its stylesheet response body
  varies by user agent, so no fixed `integrity` hash can match. Stating the exception in
  the module prevents a future "fix" that adds a hash and breaks font loading.
- **The dirty-row pattern is split across two modules.** The row's *shape* (working value
  plus saved value, `IsDirty` derived by ordinal comparison, Save/Revert gated on it) is a
  UI view-model concern and lives in `CLAUDE-blazor-ui.md`. The *outcome types* those
  actions call into are already governed by `CLAUDE-domain-driven-design.md` and stay
  there, cross-referenced. Neither file needs to know much about the other.
- **`-IncludeBlazorUi` implies `-IncludeDdd`.** The module cross-references the DDD file
  for typed outcomes. Either the bootstrap script pulls both or the README documents the
  pairing; the script is preferable because a missing cross-reference is silent.
- **`ProgramRow` gets an explicit constructor rather than an exemption to the rule.** The
  baseline's "No primary constructors" rule has two rationales: there is nowhere to put a
  null guard, and captured parameters stay in scope for the whole class body. The first
  genuinely does not apply -- `ProgramRow` is `private sealed` and nested, and the baseline
  only requires guards on public/internal entry points. The second bites hard here: `name`
  initializes *two* properties, and `SavedName` is the one `IsDirty` compares against. A
  later edit reaching for `name` instead of `SavedName` compiles, reads correctly, and
  silently breaks dirty-tracking so the row stops offering Save after its first save.
  A "private nested types are exempt" carve-out was considered and rejected: it keys on
  where a type sits rather than on whether the hazard applies, so it would also license
  primary constructors on private nested types that do have guardable inputs. Promoting
  the dirty-row pattern into a shared conventions file while its only implementation
  violates a sibling convention would hand the next agent two rules and no way to rank
  them.

## Risks / Trade-offs

- [Roughly 40 lines added to every session's context, forever] -> Mitigation: the module is
  value-free and prose-free; the "why" lives in `docs/ui-decisions.md`, which is not
  loaded. The Styling section is the weakest -- closest to restating what Bootstrap already
  implies -- and is the first thing to cut if the file needs to shrink.
- [Three of the module's sections encode a pattern used exactly once] -> Mitigation:
  asset delivery, icons, and navigation each map to a bug or cleanup that actually shipped
  in v0.0.7/v0.0.8 and are proven. Testability and editable-rows are prospective, and
  betting on them is cheap to reverse -- deleting a section from a synced file costs
  nothing.
- [Cross-repo dependency: this change is blocked on a claude-templates PR] -> Mitigation:
  the templates change is additive (one new file, one script flag, one README row) and
  breaks no existing consumer, so it can land immediately and independently.
- [pr-center stays drifted until a follow-on lands] -> Mitigation: the drift is cosmetic
  (a two-patch Bootstrap gap) plus one genuine gap (missing SRI). Filing the issue in the
  same breath as this change is what stops it from continuing indefinitely; leaving it
  unfiled is the actual risk.
