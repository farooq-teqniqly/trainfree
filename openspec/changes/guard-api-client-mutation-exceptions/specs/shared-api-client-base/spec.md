## ADDED Requirements

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
etc.), not wrapping the whole call in a generic exception guard, and issue #52 (which
raised this requirement) explicitly named `ApiClientBase` as the natural home for it.

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

- **A single generic `ExecuteAsync<TOutcome>` guard on `ApiClientBase`, not a per-client
  try/catch:** issue #52 itself proposed catching in `ApiClientBase` rather than per page
  or per client, and a generic wrapper taking the operation delegate, a `Failed` outcome
  factory, and a caller-facing message avoids nine near-duplicate catch blocks. This
  narrowly overrides `shared-api-client-base`'s prior decision against sharing
  create/rename/delete boilerplate -- that decision was about the entity-specific
  success-path branching, not this exception-safety net, and stays otherwise in force:
  the success-path DTO/outcome branching remains per client.
- **Same exception set as the existing `OnInitializedAsync` guard, including
  `OperationCanceledException`:** consistency with the load-path guard already in
  `Programs.razor`/`Sessions.razor`/`Phases.razor` means a reviewer only needs to learn
  one exception list for the whole app, not a narrower one for mutations. Revisit if a
  future case needs a canceled mutation to propagate instead of degrading to a `Failed`
  outcome (for example, distinguishing user-initiated navigation-away cancellation from a
  server-side timeout).

## Requirement coverage

Anchor: issue #52 (Add/Rename/Delete handlers in admin pages don't catch transport/parse
exceptions)

| # | Anchor requirement | Covered by |
|---|--------------------|-----------|
| 1 | A thrown transport/parse exception from `CreateXAsync`/`RenameXAsync`/`DeleteXAsync` results in the existing `XFailed`-outcome UI path, not an unhandled exception | Req: Mutation methods surface transport/parse failures as outcomes |
| 2 | Covers all three current clients (Programs, Sessions, Categories) and whatever future ones land | Req: Guard is implemented once, shared by every client -- "Categories" is `PhasesApiClient` today (`CategoriesApiClient` was renamed in #60, before this change), not a scope change made here |
| 3 | A test exists per client proving a thrown exception from the underlying `HttpClient` call surfaces as a `Failed` outcome rather than propagating | Not covered in spec -- test-coverage obligation belongs in tasks.md, not a spec requirement |
