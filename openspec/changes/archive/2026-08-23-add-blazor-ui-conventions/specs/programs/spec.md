## MODIFIED Requirements

### Requirement: Admin program list UI
The Blazor admin page SHALL display all programs as rows and allow creating, renaming,
and deleting them without a full page reload. A row with unsaved name edits SHALL offer
both `Save` and `Revert`; `Revert` discards the edit locally without calling the API.

#### Scenario: Page loads with existing programs
- **WHEN** the admin page loads
- **THEN** it calls `GET /api/programs` and renders one row per returned program,
  showing its name

#### Scenario: Adding a program
- **WHEN** the admin user clicks `[+ Program]`
- **THEN** the page calls `POST /api/programs`, appends the returned program as a new
  row, and places the name cell in an editable state

#### Scenario: Renaming a program
- **WHEN** the admin user edits a program row's name and clicks that row's `Save`
  button
- **THEN** the page calls `PATCH /api/programs/:id` with the new name and updates the
  row on success

#### Scenario: Save button hidden with no unsaved changes
- **WHEN** a program row's name has not been edited since the last successful save
- **THEN** that row's `Save` button is not shown

#### Scenario: Save button appears on edit
- **WHEN** the admin user edits a program row's name to a value different from the
  last-saved value
- **THEN** that row's `Save` button becomes visible

#### Scenario: Save button hides again after a successful save
- **WHEN** a `Save` click succeeds
- **THEN** that row's `Save` button is hidden again

#### Scenario: Revert button hidden with no unsaved changes
- **WHEN** a program row's name has not been edited since the last successful save
- **THEN** that row's `Revert` button is not shown

#### Scenario: Revert button appears on edit
- **WHEN** the admin user edits a program row's name to a value different from the
  last-saved value
- **THEN** that row's `Revert` button becomes visible alongside `Save`

#### Scenario: Reverting discards an unsaved edit
- **WHEN** the admin user edits a program row's name and clicks that row's `Revert`
  button
- **THEN** the page restores the last-saved name in the row, hides `Save` and `Revert`,
  and makes no API call

#### Scenario: Reverting clears a validation error
- **WHEN** a program row is showing a name validation error and the admin user clicks
  that row's `Revert` button
- **THEN** the error is cleared along with the unsaved edit

#### Scenario: Deleting a program
- **WHEN** the admin user clicks a program row's `Delete` button
- **THEN** the page calls `DELETE /api/programs/:id` and removes the row from the list
  on success

#### Scenario: Save rejects a name that fails the length bound client-side
- **WHEN** the admin user clicks `Save` with a name outside the 5-100 character bound
- **THEN** the page shows a validation error on that row and does not call
  `PATCH /api/programs/:id`

#### Scenario: Save surfaces a server-side rejection without crashing
- **WHEN** a `PATCH /api/programs/:id` call made by `Save` returns `400`
- **THEN** the page shows the returned error on that row and remains usable -- it does
  not throw an unhandled exception
