## Purpose

Give every Blazor `*ApiClient` (in `Trainfree.Admin` today, `Trainfree.Workout` once
slice 8 exists) one shared, tested implementation of Worker error-response reading,
instead of each app duplicating it.

## ADDED Requirements

### Requirement: Shared error-reading behavior
A single implementation SHALL read a failed `HttpResponseMessage` from the Worker API
and produce a human-readable error message, used by every `*ApiClient` class instead of
each defining its own copy.
**Rationale**: `ProgramsApiClient`, `SessionsApiClient`, and `PhasesApiClient` (renamed
from `CategoriesApiClient` in #60) each independently defined an identical ~42-line
`ReadErrorAsync`, flagged by SonarCloud as 26.9% file duplication density on review of
#50; a fourth client (`ExercisesApiClient`, roadmap slice 6) and more after it would grow
the duplication linearly.

#### Scenario: JSON error body parsed to message
- **WHEN** a Worker response has a non-success status code, `application/json` content
  type, and a body matching `{ "error": "<message>" }`
- **THEN** the shared reader returns `<message>`

#### Scenario: Non-JSON failure response falls back to a generic message
- **WHEN** a Worker response has a non-success status code and a content type other than
  `application/json` (for example, an HTML login page returned by an expired Cloudflare
  Access session)
- **THEN** the shared reader returns a generic `"Request failed with status {code}."`
  message without attempting to parse the body

#### Scenario: Malformed JSON error body falls back to a generic message
- **WHEN** a Worker response declares `application/json` but its body cannot be parsed
  into the expected shape (truncated response, wrong shape from an intermediary)
- **THEN** the shared reader returns the generic status-code fallback message instead of
  throwing

### Requirement: Per-client log category preserved
When the shared error-reading path logs an unreadable error body, it SHALL log through
the calling client's own `ILogger<T>` category, not a category tied to the shared
implementation itself.
**Rationale**: Log filtering by client (`ILogger<ProgramsApiClient>` vs
`ILogger<SessionsApiClient>`, etc.) is relied on today; centralizing the reading logic
must not collapse those categories into one.

#### Scenario: Log entry uses calling client's category
- **WHEN** `SessionsApiClient` calls the shared error-reading path and the response body
  cannot be parsed
- **THEN** the resulting log entry's category is `Trainfree.Admin.Admin.SessionsApiClient`
  (or the calling client's own type), not the shared implementation's type

### Requirement: Shared code lives outside both apps
The shared JSON options, error-reading logic, and error DTO SHALL live in a class library
referenced by `Trainfree.Admin` (and, once it exists, `Trainfree.Workout`) -- never
defined inside either app project.
**Rationale**: `Trainfree.Admin` and `Trainfree.Workout` are deliberately independent
Blazor apps behind separate Workers; placing the shared base inside `Trainfree.Admin`
would force `Trainfree.Workout` to reference `Trainfree.Admin` to reuse it, violating the
project's no-cross-app-dependency rule.

#### Scenario: A new client reuses the shared base without new duplication
- **WHEN** a future `*ApiClient` (for example, slice 6's `ExercisesApiClient`) is added in
  either app
- **THEN** it inherits the shared base for JSON options and error reading instead of
  redefining them

## Decisions

- **New `Trainfree.ApiClients` shared library, not folded into `Trainfree.Domain`:**
  `Trainfree.Domain`'s stated purpose is value objects and IDs; HTTP/JSON plumbing is a
  distinct, unrelated concern that would blur that project's scope. Folding it in would
  become the right call only if `Trainfree.Domain`'s charter broadened to cover
  cross-cutting infrastructure generally, which it does not today.
- **Error-reading only, not the create/rename/delete outcome-mapping boilerplate:** the
  per-verb success/failure branching returns entity-specific outcome types
  (`CreateProgramSucceeded` vs `CreateSessionSucceeded`, etc.); the issue's own notes flag
  sharing that as uncertain. A generic wrapper would need delegates or generics whose
  added indirection isn't justified by three (soon four) call sites. Revisit if a fifth or
  later client makes the accumulated per-verb boilerplate large enough to outweigh that
  cost.

## Requirement coverage

Anchor: issue #51 (Extract shared HTTP error-handling from the *ApiClient classes)

| # | Anchor requirement | Covered by |
|---|--------------------|-----------|
| 1 | `JsonOptions` and `ReadErrorAsync` (and its `ErrorDto`) exist in exactly one place, used by `ProgramsApiClient`, `SessionsApiClient`, and `CategoriesApiClient` | Req: Shared error-reading behavior (client is `PhasesApiClient` -- renamed from `CategoriesApiClient` by #60 after this issue was filed) |
| 2 | Each client's own `ILogger<T>` category is preserved in the shared error-reading path | Req: Per-client log category preserved |
| 3 | All existing `*ApiClientTests` keep passing unmodified in behavior, even if test setup changes | Not covered by a spec requirement -- this is a test-suite constraint, tracked as a task in tasks.md rather than an observable behavior contract |
| 4 | SonarCloud's duplicated-lines-density finding on these files clears | Not covered by a spec requirement -- external tooling outcome, tracked as a task in tasks.md |
| 5 (expanded scope, agreed in conversation, not in original issue text) | Shared base must not create a dependency from the future `Trainfree.Workout` app onto `Trainfree.Admin` | Req: Shared code lives outside both apps |
| 6 (expanded scope, agreed in conversation) | Boundary is automatically enforced, not just documented | Covered by capability `architecture-boundaries` in this same change |
