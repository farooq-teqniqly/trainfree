## MODIFIED Requirements

### Requirement: Delete a phase

The system SHALL provide `DELETE /api/phases/:id` to remove a phase, but SHALL reject
the deletion with `409` and make no change when the phase is referenced by at least one
`session_phases` row.
**Rationale**: A phase is a global library entity (not scoped to a single program), so
deleting one that a session already uses would silently orphan that session's
reference; failing the delete is cheaper and safer than a cascade that could reach
across other users' data once multi-user support exists.

#### Scenario: Phase exists and is unused

- **WHEN** a client calls `DELETE /api/phases/:id` for an existing phase referenced by
  no `session_phases` row
- **THEN** the Worker deletes the row and responds `204`

#### Scenario: Phase does not exist

- **WHEN** a client calls `DELETE /api/phases/:id` for an `:id` with no matching phase
- **THEN** the Worker responds `404`

#### Scenario: Phase is used by a session

- **WHEN** a client calls `DELETE /api/phases/:id` for a phase referenced by at least
  one `session_phases` row
- **THEN** the Worker responds `409` with a JSON error body and makes no change

### Requirement: Admin phases page

The Blazor admin app SHALL provide a `Phases` page at `/phases` listing every phase as a
row, using the same working/saved-value dirty-row pattern as the Programs page. A
phase's `Delete` action SHALL surface the Worker's `409` usage rejection instead of
silently failing or removing the row.
**Rationale**: Extends the existing page's delete flow to handle the new `409` case
introduced by the usage guard above; it does not need a proactive "Used in" indicator
in this change, since that only mattered for a searchable picker experience already
deferred out of scope.

#### Scenario: Page loads with existing phases

- **WHEN** the Phases page loads and phases exist
- **THEN** it calls `GET /api/phases` and renders one row per returned phase

#### Scenario: Page loads with no phases

- **WHEN** the Phases page loads and no phases exist
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
- **THEN** the page shows a load-failed message and remains usable
