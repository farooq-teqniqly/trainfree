## MODIFIED Requirements

### Requirement: Delete an exercise

The system SHALL provide `DELETE /api/exercises/:id` to remove an exercise, but SHALL
reject the deletion with `409` and make no change when the exercise is referenced by at
least one `program_exercises` row. When the deleted exercise had an image, the Worker
SHALL also delete its R2 object.
**Rationale**: An exercise is a global library entity (not scoped to a single program),
so deleting one that a program exercise already uses would silently orphan that
reference -- the same reasoning already applied to `phases` when `session_phases`
started referencing them. Removing the image object with its row keeps R2 from
accumulating files nothing can reach.

#### Scenario: Exercise exists and is unused

- **WHEN** a client calls `DELETE /api/exercises/:id` for an existing exercise
  referenced by no `program_exercises` row
- **THEN** the Worker deletes the row and responds `204`

#### Scenario: Exercise does not exist

- **WHEN** a client calls `DELETE /api/exercises/:id` for an `:id` with no
  matching exercise
- **THEN** the Worker responds `404`

#### Scenario: Exercise is used by a program exercise

- **WHEN** a client calls `DELETE /api/exercises/:id` for an exercise referenced by at
  least one `program_exercises` row
- **THEN** the Worker responds `409` with a JSON error body and makes no change

#### Scenario: Deleting an exercise with an image removes the image object

- **WHEN** a client calls `DELETE /api/exercises/:id` for an unused exercise that has
  an image
- **THEN** the Worker responds `204` and the exercise's R2 object no longer exists

#### Scenario: Deleting a used exercise keeps its image

- **WHEN** a client calls `DELETE /api/exercises/:id` for an exercise that has an image
  and is referenced by a `program_exercises` row
- **THEN** the Worker responds `409` and the R2 object still exists

### Requirement: Admin exercises page

The Blazor admin app SHALL provide an `Exercises` page at `/exercises` listing every
exercise as a row, using the same working/saved-value dirty-row pattern as the Phases
page, with an image column (see the `exercise-images` capability) and no type column. An
exercise row's `Delete` action SHALL
surface the Worker's `409` usage rejection instead of silently failing or removing the
row. While the initial `GET /api/exercises` is in flight, the page SHALL render
skeleton rows instead of the empty-state view, so the empty-state illustration does not
flash on screen before every successful load.
**Rationale**: Extends the existing page's delete flow to handle the new `409` case
introduced by the usage guard above, matching how the Phases page already handles its
own `409` from the `phases` capability. Before this change, the page distinguished
"loading" from "no exercises exist" only by an empty `_rows` list, so the two states
were indistinguishable and the empty-state view always flashed first, even when
exercises existed. The image column is now present because the upload flow exists.

#### Scenario: Page shows skeleton rows while loading

- **WHEN** the Exercises page has navigated to `/exercises` and the exercises fetch has
  not yet resolved
- **THEN** the page renders skeleton rows using `SkeletonBlock` from `Trainfree.UI`,
  not the empty-state view and not the populated data table

#### Scenario: Page loads with existing exercises

- **WHEN** the Exercises page's fetch resolves and exercises exist
- **THEN** it calls `GET /api/exercises` and renders one row per returned
  exercise

#### Scenario: Page loads with no exercises

- **WHEN** the Exercises page's fetch resolves and no exercises exist
- **THEN** it renders an empty-state view with an `Add Exercise` action and
  no table

#### Scenario: Adding an exercise

- **WHEN** the admin user clicks `Add Exercise`
- **THEN** the page calls `POST /api/exercises`, appends the returned
  exercise as a new row, and places the name cell in an editable state

#### Scenario: Renaming an exercise

- **WHEN** the admin user edits an exercise row's name and clicks that row's
  `Save` button
- **THEN** the page calls `PATCH /api/exercises/:id` with the new name and
  updates the row on success

#### Scenario: Save button appears on edit and hides after save

- **WHEN** the admin user edits an exercise row's name to a value different
  from the last-saved value
- **THEN** that row's `Save` button becomes visible, and is hidden again
  after a successful save

#### Scenario: Reverting discards an unsaved edit

- **WHEN** the admin user edits an exercise row's name and clicks that row's
  `Revert` button
- **THEN** the page restores the last-saved name in the row, hides `Save`
  and `Revert`, and makes no API call

#### Scenario: Deleting an unused exercise

- **WHEN** the admin user clicks an unused exercise row's `Delete` button
- **THEN** the page calls `DELETE /api/exercises/:id` and removes the row from the list
  on success

#### Scenario: Deleting a used exercise surfaces the usage rejection

- **WHEN** the admin user clicks a used exercise row's `Delete` button
- **THEN** the page calls `DELETE /api/exercises/:id`, receives `409`, shows that
  rejection on the row, and does not remove the row

#### Scenario: Save rejects a name that fails the length bound client-side

- **WHEN** the admin user clicks `Save` on an exercise row with a name
  outside the 4-100 character bound
- **THEN** the page shows a validation error on that row and does not call
  `PATCH /api/exercises/:id`

#### Scenario: Save surfaces a server-side rejection without crashing

- **WHEN** a `PATCH /api/exercises/:id` call made by `Save` returns `400` or
  `409`
- **THEN** the page shows the returned error on that row and remains usable
  -- it does not throw an unhandled exception

#### Scenario: Load failure shows an error without crashing

- **WHEN** `GET /api/exercises` fails on page load
- **THEN** the page shows a load-failed message and remains usable, not skeleton rows
  or the empty-state view
