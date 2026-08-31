## ADDED Requirements

### Requirement: Category identifier format
Each category SHALL be identified externally by a surrogate key in the form `CAT-`
followed by 6 Crockford base32 characters (e.g. `CAT-7K2QXM`), never by the table's
internal auto-incrementing row key. All API routes, request bodies, and response bodies
use this surrogate key as `id`.

#### Scenario: Generated ID shape
- **WHEN** the Worker creates a new category
- **THEN** the generated `id` matches `CAT-` followed by exactly 6 characters from the
  alphabet `ABCDEFGHJKMNPQRSTVWXYZ23456789`

### Requirement: List categories
The system SHALL provide `GET /api/categories`, returning all categories in creation
order.

#### Scenario: No categories exist
- **WHEN** a client calls `GET /api/categories` and no categories exist
- **THEN** the Worker responds `200` with an empty JSON array

#### Scenario: Categories exist
- **WHEN** a client calls `GET /api/categories` and categories exist
- **THEN** the Worker responds `200` with a JSON array of categories ordered by
  `created_at` ascending

### Requirement: Category name length
A category's `name` SHALL be between 4 and 100 characters (inclusive) after trimming
leading/trailing whitespace. This bound applies on both create and rename.

#### Scenario: Name too short
- **WHEN** a client submits a `name` that trims to fewer than 4 characters (including
  blank/whitespace-only)
- **THEN** the Worker responds `400` with a JSON error body and makes no change

#### Scenario: Name too long
- **WHEN** a client submits a `name` that trims to more than 100 characters
- **THEN** the Worker responds `400` with a JSON error body and makes no change

#### Scenario: Name at boundaries
- **WHEN** a client submits a `name` that trims to exactly 4 or exactly 100 characters
- **THEN** the Worker accepts it

### Requirement: Category name uniqueness
A category's `name` SHALL be unique among all categories, compared case-insensitively.
This applies on both create and rename.

#### Scenario: Create with a name that already exists
- **WHEN** a client calls `POST /api/categories` with a `name` matching an existing
  category's name (case-insensitively)
- **THEN** the Worker responds `409` with a JSON error body and creates no row

#### Scenario: Rename to a name that already exists on another category
- **WHEN** a client calls `PATCH /api/categories/:id` with a `name` matching another
  category's name (case-insensitively)
- **THEN** the Worker responds `409` with a JSON error body and makes no change

#### Scenario: Rename to the category's own current name
- **WHEN** a client calls `PATCH /api/categories/:id` with a `name` matching that same
  category's current name (e.g. only a change in trimmed whitespace)
- **THEN** the Worker responds `200` -- a category does not conflict with itself

### Requirement: Create a category
The system SHALL provide `POST /api/categories` to create a new category with a
system-generated ID.

#### Scenario: Valid name provided
- **WHEN** a client calls `POST /api/categories` with a JSON body containing a `name`
  between 4 and 100 characters
- **THEN** the Worker creates a `categories` row with a generated ID and current
  timestamps, and responds `201` with the created category

#### Scenario: Name fails length bound
- **WHEN** a client calls `POST /api/categories` with a missing `name`, or a `name`
  outside the 4-100 character bound
- **THEN** the Worker responds `400` with a JSON error body and creates no row

### Requirement: Rename a category
The system SHALL provide `PATCH /api/categories/:id` to update a category's name.

#### Scenario: Valid rename
- **WHEN** a client calls `PATCH /api/categories/:id` for an existing category with a
  `name` between 4 and 100 characters
- **THEN** the Worker updates the row's `name` and `updated_at`, and responds `200` with
  the updated category

#### Scenario: Category does not exist
- **WHEN** a client calls `PATCH /api/categories/:id` for an `:id` with no matching
  category
- **THEN** the Worker responds `404`

#### Scenario: Name fails length bound on rename
- **WHEN** a client calls `PATCH /api/categories/:id` with a `name` outside the 4-100
  character bound
- **THEN** the Worker responds `400` with a JSON error body and makes no change

### Requirement: Delete a category
The system SHALL provide `DELETE /api/categories/:id` to remove a category
unconditionally -- no other table references `categories` yet, so no usage guard applies
in this capability.

#### Scenario: Category exists
- **WHEN** a client calls `DELETE /api/categories/:id` for an existing category
- **THEN** the Worker deletes the row and responds `204`

#### Scenario: Category does not exist
- **WHEN** a client calls `DELETE /api/categories/:id` for an `:id` with no matching
  category
- **THEN** the Worker responds `404`

### Requirement: Admin categories page
The Blazor admin app SHALL provide a `Categories` page at `/categories` listing every
category as a row, using the same working/saved-value dirty-row pattern as the Programs
page, with no usage indicator or delete guard.

#### Scenario: Page loads with existing categories
- **WHEN** the Categories page loads and categories exist
- **THEN** it calls `GET /api/categories` and renders one row per returned category

#### Scenario: Page loads with no categories
- **WHEN** the Categories page loads and no categories exist
- **THEN** it renders an empty-state view with an `Add Category` action and no table

#### Scenario: Adding a category
- **WHEN** the admin user clicks `Add Category`
- **THEN** the page calls `POST /api/categories`, appends the returned category as a new
  row, and places the name cell in an editable state

#### Scenario: Renaming a category
- **WHEN** the admin user edits a category row's name and clicks that row's `Save`
  button
- **THEN** the page calls `PATCH /api/categories/:id` with the new name and updates the
  row on success

#### Scenario: Save button appears on edit and hides after save
- **WHEN** the admin user edits a category row's name to a value different from the
  last-saved value
- **THEN** that row's `Save` button becomes visible, and is hidden again after a
  successful save

#### Scenario: Reverting discards an unsaved edit
- **WHEN** the admin user edits a category row's name and clicks that row's `Revert`
  button
- **THEN** the page restores the last-saved name in the row, hides `Save` and `Revert`,
  and makes no API call

#### Scenario: Deleting a category
- **WHEN** the admin user clicks a category row's `Delete` button
- **THEN** the page calls `DELETE /api/categories/:id` and removes the row from the list
  on success -- no confirmation prompt or disabled state, since no usage guard exists in
  this capability

#### Scenario: Save rejects a name that fails the length bound client-side
- **WHEN** the admin user clicks `Save` on a category row with a name outside the 4-100
  character bound
- **THEN** the page shows a validation error on that row and does not call
  `PATCH /api/categories/:id`

#### Scenario: Save surfaces a server-side rejection without crashing
- **WHEN** a `PATCH /api/categories/:id` call made by `Save` returns `400` or `409`
- **THEN** the page shows the returned error on that row and remains usable -- it does
  not throw an unhandled exception

#### Scenario: Load failure shows an error without crashing
- **WHEN** `GET /api/categories` fails on page load
- **THEN** the page shows a load-failed message and remains usable
