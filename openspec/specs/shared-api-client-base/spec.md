# shared-api-client-base Specification

## Purpose

Give every Blazor `*ApiClient` (in `Trainfree.Admin` today, `Trainfree.Workout` once
slice 8 exists) one shared, tested implementation of Worker error-response reading,
instead of each app duplicating it.

## Requirements

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

### Requirement: Mutation methods surface transport/parse failures as outcomes
Every `*ApiClient` create/rename/delete method SHALL catch `HttpRequestException`,
`JsonException`, `InvalidOperationException`, `NotSupportedException`, and
`OperationCanceledException` raised while sending the request or reading the response,
and return that method's existing `Failed` outcome variant instead of letting the
exception propagate out of the calling Blazor event handler.
**Rationale**: `OnInitializedAsync` already catches this same exception set around the
initial `GetXAsync()` call so a transport blip or an expired Cloudflare Access session
(redirected to an HTML login page) degrades to a load-error message instead of blanking
the page; `CreateXAsync`/`RenameXAsync`/`DeleteXAsync` go through the same `HttpClient`
and can throw the same exceptions, but today nothing catches them there, so the same
failure mode instead crashes out of the event handler.

#### Scenario: Transport failure during create is reported, not thrown
- **WHEN** `CreateProgramAsync`'s underlying `HttpClient` call throws
  `HttpRequestException`
- **THEN** `CreateProgramAsync` returns a `CreateProgramFailed` outcome carrying a
  caller-facing message instead of the exception propagating

#### Scenario: Expired session during rename is reported, not thrown
- **WHEN** `RenameSessionAsync`'s response body cannot be parsed as JSON because
  Cloudflare Access redirected the request to an HTML login page, and reading it throws
  `JsonException`
- **THEN** `RenameSessionAsync` returns a `RenameSessionFailed` outcome instead of the
  exception propagating

#### Scenario: Cancellation during delete is reported, not thrown
- **WHEN** `DeletePhaseAsync`'s request is canceled and throws `OperationCanceledException`
- **THEN** `DeletePhaseAsync` returns a `DeletePhaseFailed` outcome instead of the
  exception propagating

### Requirement: Guard is implemented once, shared by every client
The exception-catching and outcome-mapping behavior SHALL be implemented once in
`ApiClientBase` and reused by every `*ApiClient` mutation method, rather than each
client (or each method) duplicating its own try/catch block.
**Rationale**: nine near-identical try/catch blocks (three verbs across three clients
today, more as future clients land) is exactly the duplication this capability exists to
prevent for error-reading; the same reasoning applies to this second cross-cutting
concern. This overrides this spec's earlier `## Decisions` entry that scoped the shared
base to error-reading only and left per-verb outcome-mapping unshared -- that entry
addressed sharing the entity-specific success-path branching (DTO parsing, `ToSummary`,
etc.), not wrapping the whole call in a generic exception guard.

#### Scenario: A new client's mutation methods get the guard for free
- **WHEN** a future `*ApiClient` (for example, slice 6's `ExercisesApiClient`) adds a
  create/rename/delete method built on the shared guard
- **THEN** it is protected against transport/parse exceptions without writing its own
  try/catch

### Requirement: Guarded failures are logged under the calling client's category
The shared guard SHALL log a `Warning`-level entry through the calling client's own
`ILogger<T>` category when it catches an exception and converts it to a `Failed`
outcome, consistent with how the existing error-reading path already preserves
per-client log categories.
**Rationale**: a silent catch that suppresses an exception without logging hides a real
failure path, and collapsing all clients' guarded failures into one shared log category
would break the existing per-client log filtering.

#### Scenario: Guarded exception logs under the calling client's category
- **WHEN** `ProgramsApiClient.CreateProgramAsync` throws `HttpRequestException` and the
  shared guard catches it
- **THEN** the resulting log entry's category is
  `Trainfree.Admin.Admin.ProgramsApiClient`, not the shared implementation's type

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
  cost. **Narrowed by issue #52**: a shared exception guard was added afterward (see the
  `ExecuteAsync<TOutcome>` decision below) -- that guard wraps the whole call including
  the success-path branching, but the branching itself (DTO parsing, `ToSummary`, etc.)
  remains per client, so this decision's original reasoning still holds for that part.
- **A single generic `ExecuteAsync<TOutcome>` guard on `ApiClientBase`, not a per-client
  try/catch:** issue #52 itself proposed catching in `ApiClientBase` rather than per page
  or per client, and a generic wrapper taking the operation delegate, a `Failed` outcome
  factory, and a caller-facing message avoids nine near-duplicate catch blocks. This
  narrowly overrides the decision above -- that decision was about the entity-specific
  success-path branching, not this exception-safety net, and stays otherwise in force.
- **Same exception set as the existing `OnInitializedAsync` guard, including
  `OperationCanceledException`:** consistency with the load-path guard already in
  `Programs.razor`/`Sessions.razor`/`Phases.razor` means a reviewer only needs to learn
  one exception list for the whole app, not a narrower one for mutations. Revisit if a
  future case needs a canceled mutation to propagate instead of degrading to a `Failed`
  outcome (for example, distinguishing user-initiated navigation-away cancellation from a
  server-side timeout).
