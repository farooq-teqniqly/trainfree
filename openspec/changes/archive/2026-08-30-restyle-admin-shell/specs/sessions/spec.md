## MODIFIED Requirements

### Requirement: Admin session rows nested under their program
The Blazor admin page SHALL display each program's sessions as rows nested beneath
that program's row, and allow creating, renaming, and deleting them without a full
page reload, using the same working/saved-value dirty-row pattern as program rows.
Each program's sessions SHALL be independently expandable/collapsible via a chevron on
that program's row; sessions render only while their program is expanded.

#### Scenario: Page loads with existing sessions
- **WHEN** the admin page loads and a program has sessions
- **THEN** it calls `GET /api/programs/:programId/sessions` for that program and
  renders one row per returned session, nested beneath that program's row

#### Scenario: One program's sessions fail to load
- **WHEN** the admin page loads and `GET /api/programs/:programId/sessions` fails for
  one program
- **THEN** that program's row still renders (with no sessions) alongside a per-row
  load-failed message, and every other program's row and sessions still render
  normally

#### Scenario: Adding a session
- **WHEN** the admin user clicks a program's `Add Session` action
- **THEN** the page calls `POST /api/programs/:programId/sessions`, appends the
  returned session as a new row under that program, and places the name cell in an
  editable state

#### Scenario: Renaming a session
- **WHEN** the admin user edits a session row's name and clicks that row's `Save`
  button
- **THEN** the page calls `PATCH /api/programs/:programId/sessions/:id` with the new
  name and updates the row on success

#### Scenario: Save button appears on edit and hides after save
- **WHEN** the admin user edits a session row's name to a value different from the
  last-saved value
- **THEN** that row's `Save` button becomes visible, and is hidden again after a
  successful save

#### Scenario: Reverting discards an unsaved edit
- **WHEN** the admin user edits a session row's name and clicks that row's `Revert`
  button
- **THEN** the page restores the last-saved name in the row, hides `Save` and
  `Revert`, and makes no API call

#### Scenario: Deleting a session
- **WHEN** the admin user clicks a session row's `Delete` button
- **THEN** the page calls `DELETE /api/programs/:programId/sessions/:id` and removes
  the row from the list on success

#### Scenario: Save rejects a name that fails the length bound client-side
- **WHEN** the admin user clicks `Save` on a session row with a name outside the
  4-100 character bound
- **THEN** the page shows a validation error on that row and does not call
  `PATCH /api/programs/:programId/sessions/:id`

#### Scenario: Save surfaces a server-side rejection without crashing
- **WHEN** a `PATCH /api/programs/:programId/sessions/:id` call made by `Save` returns
  `400` or `409`
- **THEN** the page shows the returned error on that row and remains usable -- it does
  not throw an unhandled exception

#### Scenario: A program starts expanded
- **WHEN** the admin page loads
- **THEN** every program's sessions are visible by default (no program starts
  collapsed)

#### Scenario: Collapsing a program hides its sessions
- **WHEN** the admin user clicks an expanded program's chevron
- **THEN** that program's session rows stop rendering, the `Add Session` action
  remains available in the program row, and the chevron's orientation reflects the
  collapsed state

#### Scenario: Expanding a collapsed program shows its sessions again
- **WHEN** the admin user clicks a collapsed program's chevron
- **THEN** that program's session rows render again, in the same order as before
  collapsing, with no re-fetch from the API

#### Scenario: Collapsing one program does not affect others
- **WHEN** the admin user collapses one program with multiple programs loaded
- **THEN** every other program's expanded/collapsed state is unchanged
