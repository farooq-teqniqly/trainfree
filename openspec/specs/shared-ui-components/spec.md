# shared-ui-components Specification

## Purpose

Houses Blazor components shared by more than one Trainfree client app -- starting
with a skeleton-loading primitive -- so `Trainfree.Admin` and the future
`Trainfree.Workout` app consume one implementation instead of each growing its own.

## Requirements

### Requirement: Trainfree.UI shared component library
A `Trainfree.UI` Razor Class Library SHALL exist as a project any Blazor WebAssembly
client app in this repo can reference, alongside `Trainfree.Domain` and
`Trainfree.Versioning`.
**Rationale**: `Trainfree.Workout` does not exist yet (roadmap slice 8+), but the
issue this change addresses explicitly asks for a component both apps can share.
Building the shared project now, with `Trainfree.Admin` as its first consumer, avoids
a later extraction PR that moves the component out of `Trainfree.Admin` and rewires
every call site.

#### Scenario: Library has no consuming-app dependency
- **WHEN** `Trainfree.UI` is built on its own
- **THEN** it does not reference `Trainfree.Admin` or any other consuming app project,
  so any Blazor WebAssembly client can reference it without a circular dependency

### Requirement: SkeletonBlock component
`Trainfree.UI` SHALL provide a `SkeletonBlock` Razor component that renders a single
placeholder shape using Bootstrap's built-in `.placeholder` utility class, accepting a
`Width` parameter (any valid CSS width value, e.g. `"60%"` or `"3rem"`) and an optional
`Height` parameter. It SHALL apply no shimmer animation itself; a shimmer effect comes
from a `.placeholder-wave` class on an ancestor element, left to the consuming page's
markup so multiple `SkeletonBlock` instances inside one shimmering region do not each
run a redundant animation.
**Rationale**: Bootstrap 5.3.8 is already CDN-pinned in every Blazor client
(`CLAUDE-blazor-ui.md`'s asset-delivery rule) and ships `.placeholder` /
`.placeholder-wave` for exactly this purpose, so no third-party skeleton-loading
library or custom CSS keyframe animation is needed -- satisfying the issue's "no 3rd
party libraries aside from Bootstrap" ask for free. A low-level primitive (one shape,
one size) composes into any page layout -- Programs' nested table, Exercises' and
Phases' flat tables, and whatever the Workout runner needs later -- rather than a
higher-level component baking in a row/column shape that would only fit some of them.

#### Scenario: Renders a sized placeholder
- **WHEN** a page renders `<SkeletonBlock Width="60%" />`
- **THEN** the component renders an element with Bootstrap's `placeholder` class and an
  inline width style of `60%`

#### Scenario: Shimmer comes from the consumer's wrapper
- **WHEN** a page wraps one or more `SkeletonBlock` instances in an element carrying
  `placeholder-wave`
- **THEN** all wrapped `SkeletonBlock` instances shimmer together, and `SkeletonBlock`
  itself carries no `placeholder-wave` class

## Decisions

- **New `Trainfree.UI` Razor Class Library now, not deferred to slice 8.** Rejected
  alternative: build the component directly in `Trainfree.Admin` and extract it into a
  shared library when `Trainfree.Workout` is scaffolded. That alternative becomes right
  again if slice 8 is far enough out that carrying an unused shared project adds more
  review friction than the extraction PR it saves -- not the case today, since the
  roadmap already lists Workout as the next major app.
- **Bootstrap's built-in `.placeholder` / `.placeholder-wave` utilities, not a
  hand-rolled CSS `@keyframes` shimmer or a third-party skeleton library.** Rejected
  alternative: a custom `@keyframes` gradient-sweep animation in `app.css` (still
  zero-dependency, but reimplements what Bootstrap 5.3.8 -- already pinned -- ships).
  That alternative would become right only if Bootstrap's placeholder shimmer did not
  visually fit the brand (`CLAUDE.md`'s black `.btn-primary` / Inter typeface
  overrides), which it does not conflict with since it is a neutral gray shimmer.
- **One low-level `SkeletonBlock` primitive, not a higher-level `SkeletonTableRow` or
  per-page skeleton component.** Rejected alternative: a `SkeletonTableRow` that takes
  a column count and renders that many cells, which would fit Exercises/Phases/Programs
  today but would not obviously fit whatever layout the Workout runner needs later, and
  would still leave Programs' non-uniform column widths (`c-name`, `c-type`, `c-count`,
  etc. in `Programs.razor`'s `<colgroup>`) needing per-column `Width` overrides anyway.
  That alternative becomes right if a second and third table-shaped consumer both want
  identical uniform-width columns, at which point extracting a thin wrapper around
  `SkeletonBlock` costs little.
