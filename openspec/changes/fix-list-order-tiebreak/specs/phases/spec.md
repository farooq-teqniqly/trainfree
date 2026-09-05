## MODIFIED Requirements

### Requirement: List phases

The system SHALL provide `GET /api/phases`, returning all phases in creation order.
When two or more phases share the same `created_at` timestamp, the system SHALL break
the tie by `id` ascending, so the order is deterministic across repeated calls.
**Rationale**: `created_at` is a millisecond-resolution string set via
`new Date().toISOString()`; two inserts within the same millisecond tie on that
column, and SQLite gives no ordering guarantee between tied rows.

#### Scenario: No phases exist

- **WHEN** a client calls `GET /api/phases` and no phases exist
- **THEN** the Worker responds `200` with an empty JSON array

#### Scenario: Phases exist

- **WHEN** a client calls `GET /api/phases` and phases exist
- **THEN** the Worker responds `200` with a JSON array of phases ordered by `created_at`
  ascending

#### Scenario: Two phases share the same created_at

- **WHEN** a client calls `GET /api/phases` and two phases have an identical
  `created_at` value
- **THEN** the Worker responds `200` with those two phases ordered by `id` ascending
  relative to each other, and this order is the same on every call
