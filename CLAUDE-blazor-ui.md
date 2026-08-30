# Blazor UI conventions

Front-end conventions for Blazor WebAssembly apps. Pairs with
`CLAUDE-domain-driven-design.md` for the outcome types editable-row actions call into.
No project-specific values here (palette, typeface, version pins) -- those belong in the
consuming repo's `CLAUDE.md`.

## Asset delivery

- Pin third-party CSS/JS to an exact version from a CDN, with a Subresource Integrity
  (`integrity`) hash and `crossorigin="anonymous"`. Never vendor a copy under `wwwroot`
  that can drift from the pinned version.
- Exception: a web font stylesheet whose response body varies by user agent (e.g. Google
  Fonts) cannot carry a fixed SRI hash -- do not add one; add `preconnect` hints to the
  font host(s) instead.

## Icons

- Use the icon font already loaded on the page (e.g. Bootstrap Icons) instead of inline
  `<svg>` markup. An inline SVG duplicates an icon the font already provides and drifts
  from it silently.
- Exception: a brand/wordmark mark with no icon-font equivalent (e.g. a product logo)
  may stay inline SVG -- it isn't duplicating something the font already provides.
- Mark decorative icons `aria-hidden="true"`. When an icon is the only content of a
  control, pair it with a `visually-hidden` text label.

## Navigation and accessibility

- Use `NavLink`, never a plain `<a href="">`, for in-app navigation. An empty or
  mistyped `href` on a plain anchor resolves to the current document instead of routing
  -- `NavLink` fails to compile without one.
- Every interactive control needs an accessible name: visible text, `title`, or a
  `visually-hidden` label -- never an icon alone.

## Testability

- Interactive elements a test needs to target carry a `data-testid` attribute. For an
  element repeated per row, suffix the id with the row's identifier
  (`data-testid="save-@row.Id"`) so tests can address one row without ambiguity.

## Editable rows

- A row with local unsaved edits is a working-value/saved-value pair: a mutable working
  property, a separately-held last-saved value, and an `IsDirty` flag derived by
  comparing the two with an explicit `StringComparison` (never implicit culture
  comparison for identifiers). Save and Revert are both gated on `IsDirty`.
- `Save` persists the working value and, on success, updates the saved value too. Save
  failures update the working value's own error state and do not touch the saved value.
- `Revert` discards the working value back to the saved value, clears the row's error
  state, and makes no API call.
- The outcome types these actions call into (success/failure as distinct types, not a
  thrown exception) are a domain-modeling concern -- see
  `CLAUDE-domain-driven-design.md`.

## Styling

- Prefer the CSS framework's existing utility classes and component patterns over new
  custom CSS. Add project-specific overrides in one app-level stylesheet, not scattered
  inline styles.
