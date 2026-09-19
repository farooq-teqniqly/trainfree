## 1. Scaffold Trainfree.UI

- [ ] 1.1 Create `src/Trainfree.UI/Trainfree.UI.csproj` as a `Microsoft.NET.Sdk.Razor`
      project mirroring `src/Trainfree.Versioning/Trainfree.Versioning.csproj`
      (`PackageReference` to `Microsoft.AspNetCore.Components.Web`,
      `InternalsVisibleTo` for `Trainfree.UI.Tests` and `DynamicProxyGenAssembly2`),
      plus a `_Imports.razor`. Verify `dotnet build src/Trainfree.UI` succeeds with no
      project references to `Trainfree.Admin` or any other app project.
- [ ] 1.2 Create `tests/Trainfree.UI.Tests/Trainfree.UI.Tests.csproj` mirroring
      `tests/Trainfree.Versioning.Tests/Trainfree.Versioning.Tests.csproj` (a
      `ProjectReference` to `Trainfree.UI` and a `PackageReference` to `bunit`). Verify
      `dotnet test tests/Trainfree.UI.Tests` runs (0 tests, green) before any test
      exists.
- [ ] 1.3 Register both new projects in `Trainfree.slnx` alongside the existing
      `Trainfree.Versioning` / `Trainfree.Versioning.Tests` entries, and add a
      `Trainfree.UI` project reference to `src/Trainfree.Admin/Trainfree.Admin.csproj`.
      Verify `dotnet build Trainfree.slnx` succeeds.
- [ ] 1.4 Extend `tests/Trainfree.ArchitectureTests/ArchitectureBoundariesTests.cs` for
      the new shared library: add a `ProjectReference` to `Trainfree.UI` in
      `Trainfree.ArchitectureTests.csproj`; add `"Trainfree.UI"` to the
      `SharedLibraryProject_HasNoProjectReferenceToAdmin` theory's `[InlineData]` list;
      add `Trainfree.UI`'s assembly (e.g. `typeof(SkeletonBlock).Assembly`) to the
      `_architecture` loader's `LoadAssemblies` call; and add
      `.Or().ResideInNamespaceMatching(@"^Trainfree\.UI(\..+)?$")` to the
      `SharedLibraries_DoNotDependOnAdmin` rule. This is what actually enforces the
      shared-ui-components spec's "Library has no consuming-app dependency" scenario --
      the csproj-text check catches an unused `ProjectReference` to Admin, and the
      ArchUnitNET rule catches any of `Trainfree.UI`'s types compiling against an Admin
      type. Verify `dotnet test tests/Trainfree.ArchitectureTests` passes, then verify
      it fails if you temporarily add a throwaway `Trainfree.Admin` reference to
      `Trainfree.UI.csproj` (revert the throwaway change after confirming the failure).

## 2. SkeletonBlock component

- [ ] 2.1 Write a failing bUnit test in `tests/Trainfree.UI.Tests/SkeletonBlockTests.cs`
      asserting `<SkeletonBlock Width="60%" />` renders an element with the
      `placeholder` CSS class and an inline `width: 60%` style. Confirm it fails to
      compile/run (no component exists yet).
- [ ] 2.2 Implement `src/Trainfree.UI/SkeletonBlock.razor` with `[EditorRequired]`
      `Width` (`string`) and optional `Height` (`string?`) parameters, rendering a
      single element carrying Bootstrap's `placeholder` class and no
      `placeholder-wave` class of its own (per the spec's Decisions: shimmer comes from
      an ancestor wrapper). Verify the 2.1 test now passes.
- [ ] 2.3 Add a bUnit test asserting two `SkeletonBlock` instances rendered inside a
      parent element carrying `placeholder-wave` do not each add their own
      `placeholder-wave` class. Verify it passes against the 2.2 implementation.
- [ ] 2.4 Add XML docs (`<summary>`, `<param>`) to `SkeletonBlock`'s `[Parameter]`
      properties per the baseline's documentation rule. Verify
      `dotnet build src/Trainfree.UI` has no `CS1591` warnings.

## 3. Exercises page loading state

- [ ] 3.1 Write a failing bUnit test in `ExercisesPageTests.cs` that holds the
      `GetExercisesAsync` task open (a `TaskCompletionSource`), renders `<Exercises>`,
      and asserts skeleton rows render (`.placeholder` present) while neither the
      empty-state view (`[data-testid=exercises-empty]`) nor a data row is shown.
      Confirm it fails against current behavior (empty-state view shows instead).
- [ ] 3.2 Add an `_isLoading` field to `Exercises.razor`, set `true` before the
      `GetExercisesAsync` call and `false` in every exit path (success and the existing
      catch block), and branch the view on `_isLoading` before the existing
      `_rows.Count == 0` check so skeleton rows render first. Verify the 3.1 test now
      passes.
- [ ] 3.3 Update the existing `OnInitialized_NoExercises_ShowsEmptyState` and
      `OnInitialized_ServerReturns...` tests if needed so they still pass now that
      loading and empty are distinct states (they resolve the fake API call
      synchronously, so `_isLoading` should already be `false` by the time bUnit's
      `Render` returns). Verify `dotnet test tests/Trainfree.Admin.Tests --filter
      FullyQualifiedName~ExercisesPageTests` is green.

## 4. Phases page loading state

- [ ] 4.1 Write a failing bUnit test in `PhasesPageTests.cs` mirroring 3.1 for
      `GetPhasesAsync`, asserting skeleton rows render while the fetch is pending and
      neither `[data-testid=phases-empty]` nor a data row is shown. Confirm it fails
      against current behavior.
- [ ] 4.2 Apply the same `_isLoading` change from 3.2 to `Phases.razor`. Verify the 4.1
      test now passes.
- [ ] 4.3 Verify the existing `PhasesPageTests` suite (empty-state and load-error
      tests) stays green: `dotnet test tests/Trainfree.Admin.Tests --filter
      FullyQualifiedName~PhasesPageTests`.

## 5. Programs page loading state

- [ ] 5.1 Write a failing bUnit test in `ProgramsPageTests.cs` that holds the program
      tree fetch open and asserts skeleton rows render inside `tbody` (matching the
      table's `<colgroup>` column count) while no data row and no `<ProgramTableRow>`
      content is shown. Confirm it fails against current behavior (empty `tbody`).
- [ ] 5.2 Add an `_isLoading` field to `Programs.razor`, set `true` at the start of
      `OnInitializedAsync` and `false` after the `Task.WhenAll` of the phase library,
      exercise library, and program tree loads completes (matching the existing
      `LoadProgramTreeAsync` internal `Task.WhenAll` synchronization comment). Render a
      fixed number of skeleton `<tr>` rows, each composed of `SkeletonBlock` instances
      per `<col>`, while `_isLoading` is `true`. Verify the 5.1 test now passes.
- [ ] 5.3 Verify the existing `ProgramsPageTests` suite stays green: `dotnet test
      tests/Trainfree.Admin.Tests --filter FullyQualifiedName~ProgramsPageTests`.

## 6. Shimmer wiring and cross-page check

- [ ] 6.1 Wrap each page's skeleton region (Programs' skeleton `tbody`, Phases' and
      Exercises' skeleton `tbody`) in an element carrying Bootstrap's
      `placeholder-wave` class, so all `SkeletonBlock` instances in that region
      shimmer together per the spec's "Shimmer comes from the consumer's wrapper"
      scenario.
- [ ] 6.2 Run `dotnet test Trainfree.slnx --configuration Release` and confirm the full
      suite is green.
- [ ] 6.3 Run `dotnet csharpier check .` and fix any formatting drift introduced by the
      above.
- [ ] 6.4 Start the Admin app locally (per README's local-dev steps) and visually
      confirm the shimmer renders on `/programs`, `/phases`, and `/exercises` during
      load, and that it is replaced by real content or the empty-state view once the
      fetch resolves.
