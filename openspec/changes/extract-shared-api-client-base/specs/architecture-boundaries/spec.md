## Purpose

Automatically enforce that shared class libraries never depend on an app-specific
project, so `Trainfree.Admin` and the future `Trainfree.Workout` stay independently
deployable and cannot silently grow a dependency on each other.

## ADDED Requirements

### Requirement: Shared libraries do not depend on app projects
An automated test SHALL fail the build if any shared class library
(`Trainfree.Domain`, `Trainfree.Versioning`, `Trainfree.ApiClients`) references
`Trainfree.Admin` or (once it exists) `Trainfree.Workout`.
**Rationale**: The project's split-admin-workout-apps design already forbids one app
depending on the other; the same rule is silently violated if a *shared* library reaches
back into an app instead, since both apps reference the shared libraries. A code review
can miss a stray project reference; an architecture test cannot.

#### Scenario: Shared library referencing Admin fails the check
- **WHEN** `Trainfree.ApiClients` (or `Trainfree.Domain` or `Trainfree.Versioning`) adds a
  project reference to `Trainfree.Admin`
- **THEN** the architecture test suite fails, naming the offending dependency direction

#### Scenario: Admin referencing a shared library passes the check
- **WHEN** `Trainfree.Admin` references `Trainfree.Domain`, `Trainfree.Versioning`, or
  `Trainfree.ApiClients`
- **THEN** the architecture test suite passes -- this is the allowed, one-way direction
