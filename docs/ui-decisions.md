# UI decisions

Why the front-end conventions in `CLAUDE-blazor-ui.md` and `CLAUDE.md`'s Project-specific
rules were chosen, recorded once here rather than left implicit in commit messages. See
`openspec/changes/archive/2026-08-23-add-blazor-ui-conventions/design.md` for why these rules live in an
always-on module rather than an on-demand skill.

## CDN over vendored Bootstrap (#31)

Bootstrap, Bootstrap Icons, and Inter are served from CDN, pinned to an exact version
with a Subresource Integrity (SRI) hash, instead of a bundled copy under `wwwroot/lib`.
This matches the pattern already used in `pr-center`'s `App.razor`. A vendored copy has
to be manually bumped and re-verified on every update; a pinned CDN link with SRI gets
the same tamper protection without carrying the files in this repo. Google Fonts is the
one exception -- its stylesheet response varies by user agent, so no fixed hash can match
it, and `preconnect` hints are used instead.

## Inter as the brand typeface (#31)

Chosen alongside the CDN move as the app's typeface, loaded from Google Fonts.

## The Bootstrap dashboard shell (#32)

The layout follows the [Bootstrap dashboard example](https://getbootstrap.com/docs/5.0/examples/dashboard/#):
a full-width dark sticky navbar carrying the brand and version stamp, above a light
`bg-body-tertiary` sidebar. Issue #10 asked for the app to look "bootstrappy" with a left
nav exposing Home and Admin; this is the reference layout that satisfies it without
inventing a bespoke design.

_Superseded 2026-08-30: the sidebar's "Home and Admin" description below is out of date --
see "Sheet-style admin restyle and the /admin -> /programs rename" further down._

## The empty `<a href="">` bug and why `NavLink` is mandatory (#32)

The navbar brand was originally a raw `<a href="">`. An empty `href` resolves to the
current document URL in browsers, not the site root, so clicking the brand while on
`/admin` stayed on `/admin` instead of routing home. The fix swapped it for a `NavLink`,
which is base-path aware and cannot compile without a real `href`. `CLAUDE-blazor-ui.md`
makes `NavLink` mandatory for in-app navigation specifically because this class of bug
compiles cleanly and only fails at runtime, on click.

## Bootstrap Icons over inline SVG (#33)

Admin page icons (add, save, delete) were originally inline `<svg>` markup. Bootstrap
Icons was already loaded from CDN for the sidebar nav, so the inline SVGs were duplicate
copies of icons the font already provided, with no dependency benefit and a drift risk
between the two representations. Replaced with the `bi bi-*` icon font classes.

## Black primary and the mobile toggle position (#34)

`.btn-primary` was restyled to black (`#000`, `#1a1a1a` on hover) instead of Bootstrap's
default blue, overridden in `app.css`. The mobile hamburger nav toggle was moved to the
left of the brand text -- it had been rendering between the brand and the version
indicator, which read as misplaced on narrow viewports.

## The Save/Revert dirty-row affordance (#35)

Editing a program name shows both `Save` and `Revert` once the row's working value
differs from its last-saved value; `Revert` restores the saved value and clears any
validation error without calling the API. This shipped as a plain `fix` PR without an
OpenSpec change -- `openspec/changes/archive/2026-08-23-add-blazor-ui-conventions/specs/programs/spec.md`
backfills the four `Revert` scenarios the `programs` spec was missing. The row's
working-value/saved-value/`IsDirty` shape is generalized in `CLAUDE-blazor-ui.md`'s
Editable rows section.

## Sheet-style admin restyle and the /admin -> /programs rename (2026-08-30, restyle-admin-shell)

`docs/design/admin-mockups/` (hi-fi, already reviewed) defined the real v0.1 look for
`Trainfree.Admin`, which had shipped so far wearing the stock Blazor/Bootstrap dashboard
scaffold. The `restyle-admin-shell` OpenSpec change implements it: a fixed 240px navbar/
sidebar rail, the navbar `VersionIndicator` restyled to a pill (with a red-X stale-state
treatment), a tile-grid `Home` landing page, and the Programs page restyled from a plain
`<table>` into the mockups' bordered, depth-indented spreadsheet (`sheet-wrap`, `.table`,
`row-depth-*`) with sized icon-button Save/Revert/Delete controls. The Programs route
moves from `/admin` to `/programs` (**breaking**, no redirect -- single-user app, nothing
external to preserve) and the redundant `Admin` sidebar wrapper link is removed, so the
sidebar is now `Home` / `Programs` only (`Phases` and `Exercises` land in slices 5-6).
Each program's sessions gained per-program chevron expand/collapse -- new client-only
state (`HashSet<ProgramId>`, no persistence, no re-fetch on expand), since nothing like it
existed before. The navbar brand's dumbbell mark has no `bi-*` equivalent and stays inline
SVG, the one documented exception to `CLAUDE-blazor-ui.md`'s icon-font rule (see its Icons
section). The mockups themselves were matched *visually* using Bootstrap's existing
components and utility classes rather than porting their bespoke CSS verbatim -- the
mockups were built in a Bootstrap-less canvas sandbox, so their class names and rules
duplicate what Bootstrap already provides (buttons, cards, grid, table borders). This
matters for whoever implements slices 5-7 (Phases, Exercises, the full spreadsheet):
those mockups come from the same sandbox and invite the same copy-paste temptation --
resist it the same way.
