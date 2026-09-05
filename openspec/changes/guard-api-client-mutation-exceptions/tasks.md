## 1. Shared exception guard on `ApiClientBase`

- [x] 1.1 Write a failing `ApiClientBaseTests` case asserting that a generic
  `ExecuteAsync<TOutcome>` helper catches `HttpRequestException`, `JsonException`,
  `InvalidOperationException`, `NotSupportedException`, and `OperationCanceledException`
  thrown by the wrapped operation and returns the supplied `onFailure` outcome instead of
  the exception propagating; confirm it fails to compile/run for the right reason (the
  member does not exist yet).
- [x] 1.2 Implement `ExecuteAsync<TOutcome>(Func<Task<TOutcome>> operation,
  Func<string, TOutcome> onFailure, string failureMessage, ILogger logger,
  CancellationToken cancellationToken)` on `ApiClientBase`, null-guarding `operation`,
  `onFailure`, `failureMessage`, and `logger`, and verify the test from 1.1 passes.
  (Implemented without the `cancellationToken` parameter: the `operation` delegate
  already closes over the caller's token, so a separate unused parameter on
  `ExecuteAsync` itself would add nothing.)
- [x] 1.3 Add a `[LoggerMessage]` `Warning`-level log entry in
  `ApiClientBase.Logging.cs` for the guarded-exception path (grep existing
  `*.Logging.cs` files first for explicit `EventId` values to avoid a collision), and add
  a test asserting the log entry's category is the calling client's own type, not
  `ApiClientBase`'s -- mirroring the existing `ReadErrorAsync` category test.
- [x] 1.4 Add a test asserting the success path is unaffected: when `operation` completes
  without throwing, `ExecuteAsync` returns its result untouched and does not log.

## 2. Wire `ProgramsApiClient` through the guard

- [x] 2.1 Write a failing `ProgramsApiClientTests` case per mutation method
  (`CreateProgramAsync`, `RenameProgramAsync`, `DeleteProgramAsync`) using a fake
  `HttpMessageHandler` that throws `HttpRequestException`, asserting each returns the
  method's `Failed` outcome instead of the exception propagating.
- [x] 2.2 Rewrite `CreateProgramAsync`, `RenameProgramAsync`, and `DeleteProgramAsync` to
  route their existing request/outcome-mapping bodies through `ExecuteAsync`, supplying
  each method's own `Failed` outcome factory and a caller-facing failure message (e.g.
  `"Could not create program. Try again."`), and verify the tests from 2.1 pass alongside
  the existing `ProgramsApiClientTests` suite.

## 3. Wire `SessionsApiClient` through the guard

- [x] 3.1 Write a failing `SessionsApiClientTests` case per mutation method
  (`CreateSessionAsync`, `RenameSessionAsync`, `DeleteSessionAsync`) using a fake
  `HttpMessageHandler` that throws `JsonException` on response reading, asserting each
  returns the method's `Failed` outcome instead of the exception propagating.
- [x] 3.2 Rewrite `CreateSessionAsync`, `RenameSessionAsync`, and `DeleteSessionAsync` to
  route through `ExecuteAsync` with their own outcome factories and failure messages, and
  verify the tests from 3.1 pass alongside the existing `SessionsApiClientTests` suite.

## 4. Wire `PhasesApiClient` through the guard

- [x] 4.1 Write a failing `PhasesApiClientTests` case per mutation method
  (`CreatePhaseAsync`, `RenamePhaseAsync`, `DeletePhaseAsync`) using a fake
  `HttpMessageHandler` that throws `OperationCanceledException`, asserting each returns
  the method's `Failed` outcome instead of the exception propagating.
- [x] 4.2 Rewrite `CreatePhaseAsync`, `RenamePhaseAsync`, and `DeletePhaseAsync` to route
  through `ExecuteAsync` with their own outcome factories and failure messages, and
  verify the tests from 4.1 pass alongside the existing `PhasesApiClientTests` suite.

## 5. Verify

- [x] 5.1 Run `dotnet test Trainfree.slnx --configuration Release` and confirm the full
  solution suite passes, including bUnit component tests for `Programs.razor`,
  `Sessions.razor`, and `Phases.razor` pages that exercise these clients.
- [x] 5.2 Confirm no new or changed public/internal member is missing a null-guard or XML
  doc, per the baseline self-audit rule, by grepping the diff for new `ExecuteAsync`
  call sites and the new `ApiClientBase` member.
