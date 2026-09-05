## 1. Scaffold Trainfree.ApiClients

- [ ] 1.1 Create `src/Trainfree.ApiClients/Trainfree.ApiClients.csproj` (class library,
      net10.0, matching `Directory.Build.props` conventions) and register it in
      `Trainfree.slnx` under `/src/`; verify `dotnet build Trainfree.slnx` succeeds with
      the new empty project included
- [ ] 1.2 Create sibling `tests/Trainfree.ApiClients.Tests/Trainfree.ApiClients.Tests.csproj`
      (xUnit v3 + NSubstitute, per baseline) and register it in `Trainfree.slnx` under
      `/tests/`; verify `dotnet test Trainfree.slnx` discovers zero tests without error

## 2. TDD: shared error-reading behavior in Trainfree.ApiClients

- [ ] 2.1 Write a failing test in `Trainfree.ApiClients.Tests` asserting a JSON error body
      (`{"error": "<message>"}`) is parsed to `<message>`; confirm it fails because
      `ApiClientBase` does not exist yet
- [ ] 2.2 Add failing tests for: a non-JSON content-type response falling back to
      `"Request failed with status {code}."`, and a response declaring
      `application/json` with an unparseable body also falling back to that same message
      without throwing
- [ ] 2.3 Add a failing test asserting the emitted log entry's category matches the
      `ILogger` instance passed in (a fake/test category), not a category fixed to the
      shared implementation type
- [ ] 2.4 Implement `ApiClientBase` (public abstract class) in
      `src/Trainfree.ApiClients/ApiClientBase.cs`: `protected static readonly
      JsonSerializerOptions JsonOptions`, `protected static Task<string>
      ReadErrorAsync(HttpResponseMessage, ILogger, CancellationToken)`, private
      `ErrorDto` record -- moved from `ProgramsApiClient`'s existing implementation,
      taking `ILogger` as a parameter instead of an instance field so each caller's own
      `ILogger<T>` flows through
- [ ] 2.5 Implement the shared `[LoggerMessage]` declaration in
      `src/Trainfree.ApiClients/ApiClientBase.Logging.cs` (one static partial method,
      same message text as the three existing per-client copies); verify all tests from
      2.1-2.3 pass

## 3. Migrate the three existing API clients

- [ ] 3.1 Change `ProgramsApiClient` to inherit `ApiClientBase`; delete its own
      `JsonOptions` field, `ReadErrorAsync` method, `ErrorDto` record, and
      `ProgramsApiClient.Logging.cs`; update call sites to
      `ReadErrorAsync(response, _logger, cancellationToken)`; verify
      `ProgramsApiClientTests` passes unmodified in its assertions
- [ ] 3.2 Repeat 3.1 for `SessionsApiClient` (and `SessionsApiClientTests`)
- [ ] 3.3 Repeat 3.1 for `PhasesApiClient` (and `PhasesApiClientTests`)
- [ ] 3.4 Run `dotnet test Trainfree.slnx --configuration Release` and confirm the full
      suite is green

## 4. Trainfree.ArchitectureTests

- [ ] 4.1 Add `TngTech.ArchUnitNET` and `TngTech.ArchUnitNET.xUnitV3` package versions to
      `Directory.Packages.props`; verify `dotnet restore` succeeds
- [ ] 4.2 Create `tests/Trainfree.ArchitectureTests/Trainfree.ArchitectureTests.csproj`
      (xUnit v3, references `Trainfree.Domain`, `Trainfree.Versioning`,
      `Trainfree.ApiClients`, and `Trainfree.Admin` project assemblies so ArchUnitNET can
      load their types) and register it in `Trainfree.slnx` under `/tests/`
- [ ] 4.3 Write a test asserting types in `Trainfree.Domain`, `Trainfree.Versioning`, and
      `Trainfree.ApiClients` namespaces do not depend on types in the `Trainfree.Admin`
      namespace; verify it passes against the current (post-migration) dependency graph
- [ ] 4.4 Verify the test actually detects a violation: temporarily add a throwaway
      project reference from `Trainfree.ApiClients` to `Trainfree.Admin` (or a
      same-effect in-code reference within the test's loaded assembly set), confirm the
      test fails, then revert the throwaway change and confirm it passes again -- do not
      commit the throwaway violation
- [ ] 4.5 Add a code comment or short doc note next to the rule (in the test file) that
      it must be extended to include `Trainfree.Workout` once slice 8
      (`add-workout-runner-untimed` or earlier) scaffolds that project

## 5. Close out

- [ ] 5.1 Run `dotnet format` / csharpier check across changed and new files and fix any
      formatting drift
- [ ] 5.2 Push and confirm the next SonarCloud analysis reports no duplicated-lines-density
      finding on `ProgramsApiClient.cs`, `SessionsApiClient.cs`, or `PhasesApiClient.cs`
      (issue #51's remaining acceptance criterion that cannot be verified locally)
