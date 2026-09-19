## MODIFIED Requirements

### Requirement: Admin phases page

The Blazor admin app SHALL provide a `Phases` page at `/phases` listing every phase as a
row, using the same working/saved-value dirty-row pattern as the Programs page. A
phase's `Delete` action SHALL surface the Worker's `409` usage rejection instead of
silently failing or removing the row. While the initial `GET /api/phases` is in flight,
the page SHALL render skeleton rows instead of the empty-state view, so the empty-state
illustration does not flash on screen before every successful load.
**Rationale**: Before this change, the page distinguished "loading" from "no phases
exist" only by an empty `_rows` list, so the two states were indistinguishable and the
page always showed the empty-state view first, even when phases existed -- a visible
flash on every load, not just a blank delay.

#### Scenario: Page shows skeleton rows while loading

- **WHEN** the Phases page has navigated to `/phases` and the phases fetch has not yet
  resolved
- **THEN** the page renders skeleton rows using `SkeletonBlock` from `Trainfree.UI`,
  not the empty-state view and not the table

#### Scenario: Page loads with existing phases

- **WHEN** the Phases page's fetch resolves and phases exist
- **THEN** it calls `GET /api/phases` and renders one row per returned phase

#### Scenario: Page loads with no phases

- **WHEN** the Phases page's fetch resolves and no phases exist
- **THEN** it renders an empty-state view with an `Add Phase` action and no table

#### Scenario: Adding a phase

- **WHEN** the admin user clicks `Add Phase`
- **THEN** the page calls `POST /api/phases`, appends the returned phase as a new row,
  and places the name cell in an editable state

#### Scenario: Renaming a phase

- **WHEN** the admin user edits a phase row's name and clicks that row's `Save` button
- **THEN** the page calls `PATCH /api/phases/:id` with the new name and updates the row
  on success

#### Scenario: Save button appears on edit and hides after save

- **WHEN** the admin user edits a phase row's name to a value different from the
  last-saved value
- **THEN** that row's `Save` button becomes visible, and is hidden again after a
  successful save

#### Scenario: Reverting discards an unsaved edit

- **WHEN** the admin user edits a phase row's name and clicks that row's `Revert` button
- **THEN** the page restores the last-saved name in the row, hides `Save` and `Revert`,
  and makes no API call

#### Scenario: Deleting an unused phase

- **WHEN** the admin user clicks an unused phase row's `Delete` button
- **THEN** the page calls `DELETE /api/phases/:id` and removes the row from the list on
  success

#### Scenario: Deleting a phase that is in use

- **WHEN** the admin user clicks a phase row's `Delete` button and the Worker responds
  `409`
- **THEN** the page shows an in-use error on that row, keeps the row in the list, and
  remains usable -- it does not throw an unhandled exception

#### Scenario: Save rejects a name that fails the length bound client-side

- **WHEN** the admin user clicks `Save` on a phase row with a name outside the 4-100
  character bound
- **THEN** the page shows a validation error on that row and does not call
  `PATCH /api/phases/:id`

#### Scenario: Save surfaces a server-side rejection without crashing

- **WHEN** a `PATCH /api/phases/:id` call made by `Save` returns `400` or `409`
- **THEN** the page shows the returned error on that row and remains usable -- it does
  not throw an unhandled exception

#### Scenario: Load failure shows an error without crashing

- **WHEN** `GET /api/phases` fails on page load
- **THEN** the page shows a load-failed message and remains usable, not skeleton rows
  or the empty-state view
