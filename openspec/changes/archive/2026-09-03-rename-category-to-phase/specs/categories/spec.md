## REMOVED Requirements

### Requirement: Category identifier format
**Reason**: The `Category` entity is renamed to `Phase`; see the `phases` capability's
`Phase identifier format` requirement.
**Migration**: Replace `CAT-` surrogate keys with `PHS-` keys. No data migration is
needed -- there is no production data at rename time.

### Requirement: List categories
**Reason**: Renamed to `phases`' `List phases` requirement.
**Migration**: Callers move from `GET /api/categories` to `GET /api/phases`.

### Requirement: Category name length
**Reason**: Renamed to `phases`' `Phase name length` requirement (bounds unchanged).
**Migration**: No behavior change; requirement now lives under the `phases` capability.

### Requirement: Category name uniqueness
**Reason**: Renamed to `phases`' `Phase name uniqueness` requirement (behavior
unchanged).
**Migration**: No behavior change; requirement now lives under the `phases` capability.

### Requirement: Create a category
**Reason**: Renamed to `phases`' `Create a phase` requirement.
**Migration**: Callers move from `POST /api/categories` to `POST /api/phases`.

### Requirement: Rename a category
**Reason**: Renamed to `phases`' `Rename a phase` requirement.
**Migration**: Callers move from `PATCH /api/categories/:id` to `PATCH /api/phases/:id`.

### Requirement: Delete a category
**Reason**: Renamed to `phases`' `Delete a phase` requirement.
**Migration**: Callers move from `DELETE /api/categories/:id` to
`DELETE /api/phases/:id`.

### Requirement: Admin categories page
**Reason**: Renamed to `phases`' `Admin phases page` requirement.
**Migration**: The `/categories` admin route and `Categories.razor` page are replaced by
`/phases` and `Phases.razor`.
