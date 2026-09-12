using System.Text.Json;
using AngleSharp.Html.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Pages;
using Trainfree.Domain.Ids;
using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Tests.Admin;

public sealed class ProgramsPageTests : BunitContext
{
    private readonly IProgramsApiClient _apiClient = Substitute.For<IProgramsApiClient>();
    private readonly IProgramTreeApiClient _treeApiClient = Substitute.For<IProgramTreeApiClient>();
    private readonly ISessionsApiClient _sessionsApiClient = Substitute.For<ISessionsApiClient>();
    private readonly IPhasesApiClient _phasesApiClient = Substitute.For<IPhasesApiClient>();
    private readonly ISessionPhasesApiClient _sessionPhasesApiClient =
        Substitute.For<ISessionPhasesApiClient>();
    private readonly IExercisesApiClient _exercisesApiClient =
        Substitute.For<IExercisesApiClient>();
    private readonly IProgramExercisesApiClient _programExercisesApiClient =
        Substitute.For<IProgramExercisesApiClient>();

    private ProgramSummary? _setUpProgram;
    private SessionSummary? _setUpSession;
    private SessionPhaseSummary? _setUpSessionPhase;

    public ProgramsPageTests()
    {
        Services.AddSingleton(_apiClient);
        Services.AddSingleton(_treeApiClient);
        Services.AddSingleton(_sessionsApiClient);
        Services.AddSingleton(_phasesApiClient);
        Services.AddSingleton(_sessionPhasesApiClient);
        Services.AddSingleton(_exercisesApiClient);
        Services.AddSingleton(_programExercisesApiClient);
        _treeApiClient.GetProgramTreeAsync(Arg.Any<CancellationToken>()).Returns([]);
        _phasesApiClient.GetPhasesAsync(Arg.Any<CancellationToken>()).Returns([]);
        _exercisesApiClient.GetExercisesAsync(Arg.Any<CancellationToken>()).Returns([]);
    }

    private static ProgramTreeItem Tree(
        ProgramSummary program,
        params SessionTreeItem[] sessions
    ) => new(program, sessions);

    private static SessionTreeItem Tree(
        SessionSummary session,
        params SessionPhaseTreeItem[] phases
    ) => new(session, phases);

    private static SessionPhaseTreeItem Tree(
        SessionPhaseSummary sessionPhase,
        params IProgramExercise[] exercises
    ) => new(sessionPhase, exercises);

    [Fact]
    public void OnInitialized_ServerReturnsTheAccessLoginPage_ShowsTheLoadErrorInsteadOfFailing()
    {
        // Arrange
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns<IReadOnlyList<ProgramTreeItem>>(_ =>
                throw new JsonException("'<' is an invalid start of a value.")
            );

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid=load-programs-error]"));
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_PhaseLibraryLoadNeverCompletes_ExerciseLibraryAndProgramTreeStillLoad()
    {
        // Arrange
        var pendingPhases = new TaskCompletionSource<IReadOnlyList<PhaseSummary>>();
        _phasesApiClient.GetPhasesAsync(CancellationToken.None).Returns(pendingPhases.Task);
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A"))]);

        // Act
        var cut = Render<Programs>();

        // Assert
        _exercisesApiClient.Received(1).GetExercisesAsync(CancellationToken.None);
        _treeApiClient.Received(1).GetProgramTreeAsync(CancellationToken.None);
        Assert.Single(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_ExistingPrograms_RendersOneRowPerProgram()
    {
        // Arrange
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([
                Tree(new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A")),
                Tree(new ProgramSummary(ProgramId.Parse("PRG-BBBBBB"), "Workout B")),
            ]);

        // Act
        var cut = Render<Programs>();

        // Assert
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Workout A", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Workout B", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddProgram_ClickPlusProgram_AppendsRowInEditMode()
    {
        // Arrange
        var created = new ProgramSummary(ProgramId.Parse("PRG-CCCCCC"), "New Program");

        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([]);

        _apiClient
            .CreateProgramAsync("New Program", CancellationToken.None)
            .Returns(new CreateProgramSucceeded(created));
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-program']").Click());

        // Assert
        await _apiClient.Received(1).CreateProgramAsync("New Program", CancellationToken.None);
        var input = cut.Find("[data-testid='name-input-PRG-CCCCCC']");
        Assert.True(input.HasAttribute("autofocus"));
    }

    [Fact]
    public async Task AddProgram_ServerRejectsDuplicateName_ShowsErrorAndAddsNoRow()
    {
        // Arrange
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([]);

        _apiClient
            .CreateProgramAsync("New Program", CancellationToken.None)
            .Returns(new CreateProgramFailed("A program named \"New Program\" already exists."));
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-program']").Click());

        // Assert
        Assert.Contains(
            "A program named \"New Program\" already exists.",
            cut.Markup,
            StringComparison.Ordinal
        );
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public async Task RenameProgram_NameEditedAndSaveClicked_CallsRenameAndUpdatesDisplayedName()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var renamed = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Renamed Workout");

        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        _apiClient
            .RenameProgramAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "Renamed Workout",
                CancellationToken.None
            )
            .Returns(new RenameProgramSucceeded(renamed));
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Renamed Workout"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-PRG-AAAAAA']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .RenameProgramAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "Renamed Workout",
                CancellationToken.None
            );
        Assert.Contains("Renamed Workout", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-PRG-AAAAAA']"));
    }

    [Fact]
    public async Task RenameProgram_NameFailsLengthBound_ShowsErrorAndDoesNotCallApi()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Ab"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-PRG-AAAAAA']").Click());

        // Assert
        await _apiClient
            .DidNotReceive()
            .RenameProgramAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
        Assert.NotEmpty(cut.FindAll("[data-testid='name-error-PRG-AAAAAA']"));
    }

    [Fact]
    public async Task RenameProgram_ServerRejects_ShowsErrorWithoutThrowing()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        _apiClient
            .RenameProgramAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "Renamed Workout",
                CancellationToken.None
            )
            .Returns(new RenameProgramFailed("name must be between 4 and 100 characters"));
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Renamed Workout"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-PRG-AAAAAA']").Click());

        // Assert
        Assert.Contains(
            "name must be between 4 and 100 characters",
            cut.Markup,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task RenameProgram_EnterKeyOnDirtyRow_CallsRenameAndUpdatesDisplayedName()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var renamed = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Renamed Workout");

        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        _apiClient
            .RenameProgramAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "Renamed Workout",
                CancellationToken.None
            )
            .Returns(new RenameProgramSucceeded(renamed));
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Renamed Workout"));

        // Act
        await cut.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        // Assert
        await _apiClient
            .Received(1)
            .RenameProgramAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "Renamed Workout",
                CancellationToken.None
            );
        Assert.Contains("Renamed Workout", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-PRG-AAAAAA']"));
    }

    [Fact]
    public async Task RenameProgram_EnterKeyOnCleanRow_DoesNotCallApi()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        // Assert
        await _apiClient
            .DidNotReceive()
            .RenameProgramAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task RenameProgram_EnterKeyRepeatedWhileSaveInFlight_CallsRenameOnlyOnce()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var renamed = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Renamed Workout");

        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        var tcs = new TaskCompletionSource<RenameProgramOutcome>();
        _apiClient
            .RenameProgramAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "Renamed Workout",
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Renamed Workout"));

        // Act
        var firstKeyDown = cut.InvokeAsync(() =>
            input.KeyDown(new KeyboardEventArgs { Key = "Enter" })
        );
        await cut.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        tcs.SetResult(new RenameProgramSucceeded(renamed));
        await firstKeyDown;

        // Assert
        await _apiClient
            .Received(1)
            .RenameProgramAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "Renamed Workout",
                CancellationToken.None
            );
    }

    [Fact]
    public async Task RenameProgram_SaveInFlight_DisablesNameInputSoLaterEditsAreNotSilentlyLost()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        var tcs = new TaskCompletionSource<RenameProgramOutcome>();
        _apiClient
            .RenameProgramAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "Renamed Workout",
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Renamed Workout"));

        // Act
        var saveTask = cut.InvokeAsync(() =>
            input.KeyDown(new KeyboardEventArgs { Key = "Enter" })
        );

        // Assert
        Assert.True(cut.Find("[data-testid='name-input-PRG-AAAAAA']").HasAttribute("disabled"));

        await cut.InvokeAsync(() =>
            tcs.SetResult(new RenameProgramSucceeded(program with { Name = "Renamed Workout" }))
        );
        await saveTask;
        Assert.False(cut.Find("[data-testid='name-input-PRG-AAAAAA']").HasAttribute("disabled"));
    }

    [Fact]
    public void SaveButton_NoUnsavedChanges_IsNotShown()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='save-PRG-AAAAAA']"));
    }

    [Fact]
    public async Task SaveButton_NameEdited_BecomesVisible()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Renamed Workout"));

        // Assert
        Assert.Single(cut.FindAll("[data-testid='save-PRG-AAAAAA']"));
    }

    [Fact]
    public void RevertButton_NoUnsavedChanges_IsNotShown()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='revert-PRG-AAAAAA']"));
    }

    [Fact]
    public async Task RevertButton_NameEdited_BecomesVisible()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Renamed Workout"));

        // Assert
        Assert.Single(cut.FindAll("[data-testid='revert-PRG-AAAAAA']"));
    }

    [Fact]
    public async Task RevertProgram_ClickRevert_RestoresOriginalNameAndHidesButtons()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Renamed Workout"));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='revert-PRG-AAAAAA']").Click());

        // Assert
        Assert.Contains("value=\"Workout A\"", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-PRG-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='revert-PRG-AAAAAA']"));
    }

    [Fact]
    public async Task RevertProgram_NameShowingValidationError_ClearsError()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Ab"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-PRG-AAAAAA']").Click());
        Assert.NotEmpty(cut.FindAll("[data-testid='name-error-PRG-AAAAAA']"));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='revert-PRG-AAAAAA']").Click());

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='name-error-PRG-AAAAAA']"));
    }

    [Fact]
    public async Task DeleteProgram_ClickDelete_CallsDeleteAndRemovesRow()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        _apiClient
            .DeleteProgramAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns(new DeleteProgramSucceeded());
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-PRG-AAAAAA']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .DeleteProgramAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None);
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public async Task DeleteProgram_ServerRejects_ShowsErrorAndKeepsRowWithoutThrowing()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        _apiClient
            .DeleteProgramAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns(new DeleteProgramFailed("Request failed with status 500."));
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-PRG-AAAAAA']").Click());

        // Assert
        Assert.Contains("Request failed with status 500.", cut.Markup, StringComparison.Ordinal);
        Assert.Single(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_LoadFails_ShowsErrorWithoutThrowing()
    {
        // Arrange
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns<Task<IReadOnlyList<ProgramTreeItem>>>(_ =>
                throw new HttpRequestException("simulated failure")
            );

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='load-programs-error']"));
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_ProgramTreeThrowsFormatException_ShowsErrorWithoutThrowing()
    {
        // Arrange
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns<Task<IReadOnlyList<ProgramTreeItem>>>(_ =>
                throw new FormatException("Unknown program exercise type: 'Bogus'.")
            );

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='load-programs-error']"));
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_ProgramTreeThrowsArgumentException_ShowsErrorWithoutThrowing()
    {
        // Arrange
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns<Task<IReadOnlyList<ProgramTreeItem>>>(_ =>
                throw new ArgumentException("Unknown program exercise side: 'Bogus'.")
            );

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='load-programs-error']"));
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_ProgramHasSessions_RendersSessionRowsNestedUnderProgram()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([
                Tree(
                    program,
                    Tree(
                        new SessionSummary(
                            SessionId.Parse("SNN-AAAAAA"),
                            ProgramId.Parse("PRG-AAAAAA"),
                            "Monday Lower Body"
                        )
                    ),
                    Tree(
                        new SessionSummary(
                            SessionId.Parse("SNN-BBBBBB"),
                            ProgramId.Parse("PRG-AAAAAA"),
                            "Wednesday Upper Body"
                        )
                    )
                ),
            ]);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Contains("Monday Lower Body", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Wednesday Upper Body", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddSession_ClickAddSession_AppendsRowInEditMode()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var created = new SessionSummary(
            SessionId.Parse("SNN-CCCCCC"),
            ProgramId.Parse("PRG-AAAAAA"),
            "New Session"
        );

        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        _sessionsApiClient
            .CreateSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "New Session",
                CancellationToken.None
            )
            .Returns(new CreateSessionSucceeded(created));
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-session-PRG-AAAAAA']").Click());

        // Assert
        await _sessionsApiClient
            .Received(1)
            .CreateSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "New Session",
                CancellationToken.None
            );
        var input = cut.Find("[data-testid='session-name-input-SNN-CCCCCC']");
        Assert.True(input.HasAttribute("autofocus"));
    }

    [Fact]
    public async Task AddSession_ClickAddSessionOnCollapsedProgram_ExpandsAndShowsNewRow()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var created = new SessionSummary(
            SessionId.Parse("SNN-CCCCCC"),
            ProgramId.Parse("PRG-AAAAAA"),
            "New Session"
        );

        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        _sessionsApiClient
            .CreateSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "New Session",
                CancellationToken.None
            )
            .Returns(new CreateSessionSucceeded(created));
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='chevron-PRG-AAAAAA']").Click());

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-session-PRG-AAAAAA']").Click());

        // Assert
        var input = cut.Find("[data-testid='session-name-input-SNN-CCCCCC']");
        Assert.True(input.HasAttribute("autofocus"));
    }

    [Fact]
    public async Task AddSession_ServerRejectsDuplicateName_ShowsErrorAndAddsNoRow()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        _sessionsApiClient
            .CreateSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "New Session",
                CancellationToken.None
            )
            .Returns(new CreateSessionFailed("A session named \"New Session\" already exists."));
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-session-PRG-AAAAAA']").Click());

        // Assert
        Assert.Contains(
            "A session named \"New Session\" already exists.",
            cut.Markup,
            StringComparison.Ordinal
        );
        Assert.Empty(cut.FindAll("[data-testid^='session-name-input-']"));
    }

    [Fact]
    public async Task RenameSession_NameEditedAndSaveClicked_CallsRenameAndUpdatesDisplayedName()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        var renamed = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Renamed Session"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        _sessionsApiClient
            .RenameSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                "Renamed Session",
                CancellationToken.None
            )
            .Returns(new RenameSessionSucceeded(renamed));
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='session-name-input-SNN-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Renamed Session"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-save-SNN-AAAAAA']").Click());

        // Assert
        await _sessionsApiClient
            .Received(1)
            .RenameSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                "Renamed Session",
                CancellationToken.None
            );
        Assert.Contains("Renamed Session", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='session-save-SNN-AAAAAA']"));
    }

    [Fact]
    public async Task RenameSession_NameFailsLengthBound_ShowsErrorAndDoesNotCallApi()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='session-name-input-SNN-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Ab"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-save-SNN-AAAAAA']").Click());

        // Assert
        await _sessionsApiClient
            .DidNotReceive()
            .RenameSessionAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
        Assert.NotEmpty(cut.FindAll("[data-testid='session-name-error-SNN-AAAAAA']"));
    }

    [Fact]
    public async Task RenameSession_ServerRejects_ShowsErrorWithoutThrowing()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        _sessionsApiClient
            .RenameSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                "Renamed Session",
                CancellationToken.None
            )
            .Returns(new RenameSessionFailed("name must be between 4 and 100 characters"));
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='session-name-input-SNN-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Renamed Session"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-save-SNN-AAAAAA']").Click());

        // Assert
        Assert.Contains(
            "name must be between 4 and 100 characters",
            cut.Markup,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task RenameSession_EnterKeyOnDirtyRow_CallsRenameAndUpdatesDisplayedName()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        var renamed = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Renamed Session"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        _sessionsApiClient
            .RenameSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                "Renamed Session",
                CancellationToken.None
            )
            .Returns(new RenameSessionSucceeded(renamed));
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='session-name-input-SNN-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Renamed Session"));

        // Act
        await cut.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        // Assert
        await _sessionsApiClient
            .Received(1)
            .RenameSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                "Renamed Session",
                CancellationToken.None
            );
        Assert.Contains("Renamed Session", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='session-save-SNN-AAAAAA']"));
    }

    [Fact]
    public async Task RenameSession_EnterKeyOnCleanRow_DoesNotCallApi()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='session-name-input-SNN-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        // Assert
        await _sessionsApiClient
            .DidNotReceive()
            .RenameSessionAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task RenameSession_EnterKeyRepeatedWhileSaveInFlight_CallsRenameOnlyOnce()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        var renamed = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Renamed Session"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        var tcs = new TaskCompletionSource<RenameSessionOutcome>();
        _sessionsApiClient
            .RenameSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                "Renamed Session",
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='session-name-input-SNN-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Renamed Session"));

        // Act
        var firstKeyDown = cut.InvokeAsync(() =>
            input.KeyDown(new KeyboardEventArgs { Key = "Enter" })
        );
        await cut.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        tcs.SetResult(new RenameSessionSucceeded(renamed));
        await firstKeyDown;

        // Assert
        await _sessionsApiClient
            .Received(1)
            .RenameSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                "Renamed Session",
                CancellationToken.None
            );
    }

    [Fact]
    public async Task RenameSession_SaveInFlight_DisablesNameInputSoLaterEditsAreNotSilentlyLost()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        var tcs = new TaskCompletionSource<RenameSessionOutcome>();
        _sessionsApiClient
            .RenameSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                "Renamed Session",
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='session-name-input-SNN-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Renamed Session"));

        // Act
        var saveTask = cut.InvokeAsync(() =>
            input.KeyDown(new KeyboardEventArgs { Key = "Enter" })
        );

        // Assert
        Assert.True(
            cut.Find("[data-testid='session-name-input-SNN-AAAAAA']").HasAttribute("disabled")
        );

        await cut.InvokeAsync(() =>
            tcs.SetResult(new RenameSessionSucceeded(session with { Name = "Renamed Session" }))
        );
        await saveTask;
        Assert.False(
            cut.Find("[data-testid='session-name-input-SNN-AAAAAA']").HasAttribute("disabled")
        );
    }

    [Fact]
    public async Task RevertSession_ClickRevert_RestoresOriginalNameAndHidesButtons()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='session-name-input-SNN-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Renamed Session"));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-revert-SNN-AAAAAA']").Click());

        // Assert
        Assert.Contains("value=\"Monday Lower Body\"", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='session-save-SNN-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='session-revert-SNN-AAAAAA']"));
        await _sessionsApiClient
            .DidNotReceive()
            .RenameSessionAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task DeleteSession_ClickDelete_CallsDeleteAndRemovesRow()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        _sessionsApiClient
            .DeleteSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns(new DeleteSessionSucceeded());
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-delete-SNN-AAAAAA']").Click());

        // Assert
        await _sessionsApiClient
            .Received(1)
            .DeleteSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            );
        Assert.Empty(cut.FindAll("[data-testid='session-name-input-SNN-AAAAAA']"));
    }

    [Fact]
    public void OnInitialized_ProgramHasSessions_StartsExpanded()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Contains("Monday Lower Body", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chevron_ClickOnExpandedProgram_SetsAriaExpandedFalse()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        var cut = Render<Programs>();
        var chevron = cut.Find("[data-testid='chevron-PRG-AAAAAA']");
        Assert.Equal("true", chevron.GetAttribute("aria-expanded"));

        // Act
        await cut.InvokeAsync(() => chevron.Click());

        // Assert
        chevron = cut.Find("[data-testid='chevron-PRG-AAAAAA']");
        Assert.Equal("false", chevron.GetAttribute("aria-expanded"));
    }

    [Fact]
    public async Task Chevron_ClickOnExpandedProgram_HidesItsSessions()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='chevron-PRG-AAAAAA']").Click());

        // Assert
        Assert.DoesNotContain("Monday Lower Body", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chevron_ClickOnCollapsedProgram_RestoresSessionsWithoutRefetch()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='chevron-PRG-AAAAAA']").Click());

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='chevron-PRG-AAAAAA']").Click());

        // Assert
        Assert.Contains("Monday Lower Body", cut.Markup, StringComparison.Ordinal);
        await _treeApiClient.Received(1).GetProgramTreeAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Chevron_CollapseOneProgram_LeavesOtherProgramsExpanded()
    {
        // Arrange
        var programA = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var programB = new ProgramSummary(ProgramId.Parse("PRG-BBBBBB"), "Workout B");
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([
                Tree(
                    programA,
                    Tree(
                        new SessionSummary(
                            SessionId.Parse("SNN-AAAAAA"),
                            ProgramId.Parse("PRG-AAAAAA"),
                            "Monday Lower Body"
                        )
                    )
                ),
                Tree(
                    programB,
                    Tree(
                        new SessionSummary(
                            SessionId.Parse("SNN-BBBBBB"),
                            ProgramId.Parse("PRG-BBBBBB"),
                            "Tuesday Upper Body"
                        )
                    )
                ),
            ]);

        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='chevron-PRG-AAAAAA']").Click());

        // Assert
        Assert.DoesNotContain("Monday Lower Body", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Tuesday Upper Body", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chevron_ClickOnProgramWithNoSessions_StaysStaticAndDisabled()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _treeApiClient.GetProgramTreeAsync(CancellationToken.None).Returns([Tree(program)]);

        var cut = Render<Programs>();
        var chevron = cut.Find("[data-testid='chevron-PRG-AAAAAA']");
        Assert.True(chevron.HasAttribute("disabled"));
        Assert.Equal("false", chevron.GetAttribute("aria-expanded"));
        Assert.Equal("No sessions", chevron.GetAttribute("title"));
        Assert.Contains("No sessions", chevron.InnerHtml, StringComparison.Ordinal);

        // Act
        await cut.InvokeAsync(() => chevron.Click());

        // Assert
        chevron = cut.Find("[data-testid='chevron-PRG-AAAAAA']");
        Assert.Equal("false", chevron.GetAttribute("aria-expanded"));
        Assert.DoesNotContain("chevron-collapsed", chevron.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteSession_ServerRejects_ShowsErrorAndKeepsRowWithoutThrowing()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        _sessionsApiClient
            .DeleteSessionAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns(new DeleteSessionFailed("Request failed with status 500."));
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-delete-SNN-AAAAAA']").Click());

        // Assert
        Assert.Contains("Request failed with status 500.", cut.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(cut.FindAll("[data-testid='session-name-input-SNN-AAAAAA']"));
    }

    [Fact]
    public void OnInitialized_SessionHasPhases_RendersPhaseRowsNestedUnderSession()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");

        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([
                Tree(
                    program,
                    Tree(
                        session,
                        Tree(
                            new SessionPhaseSummary(
                                SessionPhaseId.Parse("SPH-AAAAAA"),
                                SessionId.Parse("SNN-AAAAAA"),
                                PhaseId.Parse("PHS-AAAAAA")
                            )
                        )
                    )
                ),
            ]);

        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Contains("Warm Up", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void OnInitialized_SessionHasPhases_StartsExpanded()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");

        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([
                Tree(
                    program,
                    Tree(
                        session,
                        Tree(
                            new SessionPhaseSummary(
                                SessionPhaseId.Parse("SPH-AAAAAA"),
                                SessionId.Parse("SNN-AAAAAA"),
                                PhaseId.Parse("PHS-AAAAAA")
                            )
                        )
                    )
                ),
            ]);

        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='phase-name-SPH-AAAAAA']"));
    }

    [Fact]
    public async Task SessionChevron_ClickOnExpandedSession_HidesItsPhases()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");

        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([
                Tree(
                    program,
                    Tree(
                        session,
                        Tree(
                            new SessionPhaseSummary(
                                SessionPhaseId.Parse("SPH-AAAAAA"),
                                SessionId.Parse("SNN-AAAAAA"),
                                PhaseId.Parse("PHS-AAAAAA")
                            )
                        )
                    )
                ),
            ]);

        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);

        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-chevron-SNN-AAAAAA']").Click());

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='phase-name-SPH-AAAAAA']"));
    }

    [Fact]
    public async Task SessionChevron_ClickOnCollapsedSession_RestoresPhasesWithoutRefetch()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        var sessionPhase = new SessionPhaseSummary(
            SessionPhaseId.Parse("SPH-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA")
        );
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session, Tree(sessionPhase)))]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);

        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-chevron-SNN-AAAAAA']").Click());

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-chevron-SNN-AAAAAA']").Click());

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='phase-name-SPH-AAAAAA']"));
        await _treeApiClient.Received(1).GetProgramTreeAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SessionChevron_CollapseOneSession_LeavesOtherSessionsExpanded()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");

        var sessionA = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        var sessionB = new SessionSummary(
            SessionId.Parse("SNN-BBBBBB"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Wednesday Upper Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([
                Tree(
                    program,
                    Tree(
                        sessionA,
                        Tree(
                            new SessionPhaseSummary(
                                SessionPhaseId.Parse("SPH-AAAAAA"),
                                SessionId.Parse("SNN-AAAAAA"),
                                PhaseId.Parse("PHS-AAAAAA")
                            )
                        )
                    ),
                    Tree(
                        sessionB,
                        Tree(
                            new SessionPhaseSummary(
                                SessionPhaseId.Parse("SPH-BBBBBB"),
                                SessionId.Parse("SNN-BBBBBB"),
                                PhaseId.Parse("PHS-AAAAAA")
                            )
                        )
                    )
                ),
            ]);

        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);

        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-chevron-SNN-AAAAAA']").Click());

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='phase-name-SPH-AAAAAA']"));
        Assert.NotEmpty(cut.FindAll("[data-testid='phase-name-SPH-BBBBBB']"));
    }

    [Fact]
    public async Task AddPhase_ClickAddPhase_ShowsDropdownOfPhaseLibrary()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-phase-SNN-AAAAAA']").Click());

        // Assert
        var picker = cut.Find("[data-testid='phase-picker-SNN-AAAAAA']");
        Assert.Contains("Warm Up", picker.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddPhase_ClickAddPhase_ShowsDropdownSortedByName()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([
                new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up"),
                new PhaseSummary(PhaseId.Parse("PHS-BBBBBB"), "Cool Down"),
            ]);
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-phase-SNN-AAAAAA']").Click());

        // Assert
        var picker = cut.Find("[data-testid='phase-picker-SNN-AAAAAA']");
        var names = picker.QuerySelectorAll("option").Select(o => o.TextContent).Skip(1).ToList();
        Assert.Equal(["Cool Down", "Warm Up"], names);
    }

    [Fact]
    public async Task AddPhase_SelectPhase_CallsCreateAndAppendsRow()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _sessionPhasesApiClient
            .CreateSessionPhaseAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                PhaseId.Parse("PHS-AAAAAA"),
                CancellationToken.None
            )
            .Returns(
                new CreateSessionPhaseSucceeded(
                    new SessionPhaseSummary(
                        SessionPhaseId.Parse("SPH-CCCCCC"),
                        SessionId.Parse("SNN-AAAAAA"),
                        PhaseId.Parse("PHS-AAAAAA")
                    )
                )
            );
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-phase-SNN-AAAAAA']").Click());

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='phase-picker-SNN-AAAAAA']").Change("PHS-AAAAAA")
        );

        // Assert
        await _sessionPhasesApiClient
            .Received(1)
            .CreateSessionPhaseAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                PhaseId.Parse("PHS-AAAAAA"),
                CancellationToken.None
            );
        Assert.NotEmpty(cut.FindAll("[data-testid='phase-name-SPH-CCCCCC']"));
    }

    [Fact]
    public async Task AddPhase_EmptyPhaseLibrary_ShowsGuidanceAndMakesNoApiCall()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-phase-SNN-AAAAAA']").Click());

        // Assert
        Assert.NotNull(cut.Find("[data-testid='empty-phase-library-SNN-AAAAAA']"));
        await _sessionPhasesApiClient
            .DidNotReceive()
            .CreateSessionPhaseAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<PhaseId>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task AddPhase_PhaseLibraryFailedToLoad_ShowsLoadErrorInsteadOfEmptyGuidance()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session))]);

        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns<Task<IReadOnlyList<PhaseSummary>>>(_ =>
                throw new HttpRequestException("simulated failure")
            );
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-phase-SNN-AAAAAA']").Click());

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='phase-library-load-error-SNN-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='empty-phase-library-SNN-AAAAAA']"));
    }

    [Fact]
    public async Task DeleteSessionPhase_ClickDelete_CallsDeleteAndRemovesRow()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");

        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([
                Tree(
                    program,
                    Tree(
                        session,
                        Tree(
                            new SessionPhaseSummary(
                                SessionPhaseId.Parse("SPH-AAAAAA"),
                                SessionId.Parse("SNN-AAAAAA"),
                                PhaseId.Parse("PHS-AAAAAA")
                            )
                        )
                    )
                ),
            ]);

        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);

        _sessionPhasesApiClient
            .DeleteSessionPhaseAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                SessionPhaseId.Parse("SPH-AAAAAA"),
                CancellationToken.None
            )
            .Returns(new DeleteSessionPhaseSucceeded());
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='phase-delete-SPH-AAAAAA']").Click());

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='phase-name-SPH-AAAAAA']"));
    }

    [Fact]
    public async Task DeleteSessionPhase_ServerRejects_ShowsErrorOnRowWithoutThrowing()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");

        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );

        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([
                Tree(
                    program,
                    Tree(
                        session,
                        Tree(
                            new SessionPhaseSummary(
                                SessionPhaseId.Parse("SPH-AAAAAA"),
                                SessionId.Parse("SNN-AAAAAA"),
                                PhaseId.Parse("PHS-AAAAAA")
                            )
                        )
                    )
                ),
            ]);

        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);

        _sessionPhasesApiClient
            .DeleteSessionPhaseAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                SessionPhaseId.Parse("SPH-AAAAAA"),
                CancellationToken.None
            )
            .Returns(new DeleteSessionPhaseFailed("Request failed with status 500."));
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='phase-delete-SPH-AAAAAA']").Click());

        // Assert
        Assert.Contains("Request failed with status 500.", cut.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(cut.FindAll("[data-testid='phase-name-SPH-AAAAAA']"));
    }

    private (
        ProgramId ProgramId,
        SessionId SessionId,
        SessionPhaseId SessionPhaseId
    ) SetUpProgramSessionPhase(
        string exerciseId = "EXR-AAAAAA",
        string exerciseName = "Bodyweight Squat"
    )
    {
        _setUpProgram = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _setUpSession = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _setUpSessionPhase = new SessionPhaseSummary(
            SessionPhaseId.Parse("SPH-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA")
        );
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _exercisesApiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns([new ExerciseSummary(ExerciseId.Parse(exerciseId), exerciseName)]);
        SetProgramExercises();

        return (_setUpProgram.Id, _setUpSession.Id, _setUpSessionPhase.Id);
    }

    private void SetProgramExercises(params IProgramExercise[] exercises) =>
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([
                Tree(_setUpProgram!, Tree(_setUpSession!, Tree(_setUpSessionPhase!, exercises))),
            ]);

    [Fact]
    public void ProgramExercises_SessionPhaseHasProgramExercises_RendersRowsNestedUnderPhase()
    {
        // Arrange
        var (_, _, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Equal(
            "Bodyweight Squat",
            cut.Find("[data-testid='program-exercise-name-PGX-AAAAAA']").TextContent.Trim()
        );
    }

    [Fact]
    public async Task AddProgramExercise_ClickAddExercise_ShowsFormWithExerciseDropdown()
    {
        // Arrange
        SetUpProgramSessionPhase();
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());

        // Assert
        var picker = cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']");
        Assert.Contains("Bodyweight Squat", picker.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddProgramExercise_ClickAddExercise_ShowsDropdownSortedByName()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        var sessionPhase = new SessionPhaseSummary(
            SessionPhaseId.Parse("SPH-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA")
        );
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session, Tree(sessionPhase)))]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _exercisesApiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns([
                new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Zercher Squat"),
                new ExerciseSummary(ExerciseId.Parse("EXR-BBBBBB"), "Bodyweight Squat"),
            ]);
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());

        // Assert
        var picker = cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']");
        var names = picker.QuerySelectorAll("option").Select(o => o.TextContent).Skip(1).ToList();
        Assert.Equal(["Bodyweight Squat", "Zercher Squat"], names);
    }

    [Fact]
    public async Task AddProgramExercise_EmptyExerciseLibrary_ShowsGuidanceAndMakesNoApiCall()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        var sessionPhase = new SessionPhaseSummary(
            SessionPhaseId.Parse("SPH-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA")
        );
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session, Tree(sessionPhase)))]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());

        // Assert
        Assert.NotNull(cut.Find("[data-testid='empty-exercise-library-SPH-AAAAAA']"));
        await _programExercisesApiClient
            .DidNotReceive()
            .CreateRepsProgramExerciseAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<SessionPhaseId>(),
                Arg.Any<ExerciseId>(),
                Arg.Any<int>(),
                Arg.Any<SetPrescription>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task AddProgramExercise_MissingRequiredFields_SubmitButtonIsDisabled()
    {
        // Arrange
        SetUpProgramSessionPhase();
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());

        // Assert
        var button = cut.Find("[data-testid='add-exercise-submit-SPH-AAAAAA']");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public async Task AddProgramExercise_ClickAddExerciseWhileFormAlreadyOpen_ResetsForm()
    {
        // Arrange
        SetUpProgramSessionPhase();
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']").Change("EXR-AAAAAA")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-count-SPH-AAAAAA']").Input("10")
        );

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());

        // Assert
        Assert.True(
            string.IsNullOrEmpty(
                ((IHtmlSelectElement)cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']")).Value
            )
        );
        Assert.True(
            string.IsNullOrEmpty(
                ((IHtmlInputElement)cut.Find("[data-testid='exercise-count-SPH-AAAAAA']")).Value
            )
        );
    }

    [Fact]
    public async Task AddProgramExercise_CollapseSessionWhileFormOpen_DoesNotCollapse()
    {
        // Arrange
        SetUpProgramSessionPhase();
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-chevron-SNN-AAAAAA']").Click());

        // Assert
        Assert.Single(cut.FindAll("[data-testid='exercise-count-SPH-AAAAAA']"));
    }

    [Fact]
    public async Task AddProgramExercise_CollapseProgramWhileFormOpen_DoesNotCollapse()
    {
        // Arrange
        SetUpProgramSessionPhase();
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='chevron-PRG-AAAAAA']").Click());

        // Assert
        Assert.Single(cut.FindAll("[data-testid='exercise-count-SPH-AAAAAA']"));
    }

    [Fact]
    public async Task AddProgramExercise_CollapseSessionWithEmptyExerciseLibraryPickerOpen_Collapses()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        var sessionPhase = new SessionPhaseSummary(
            SessionPhaseId.Parse("SPH-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA")
        );
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session, Tree(sessionPhase)))]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-chevron-SNN-AAAAAA']").Click());

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='empty-exercise-library-SPH-AAAAAA']"));
    }

    [Fact]
    public async Task AddProgramExercise_SubmitValidReps_CallsCreateAndAppendsRow()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(
                new CreateProgramExerciseSucceeded(
                    new RepsProgramExercise(
                        ProgramExerciseId.Parse("PGX-CCCCCC"),
                        sessionPhaseId,
                        ExerciseId.Parse("EXR-AAAAAA"),
                        10,
                        0,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Both
                    )
                )
            );
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']").Change("EXR-AAAAAA")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-count-SPH-AAAAAA']").Input("10")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-sets-SPH-AAAAAA']").Input("3")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-restseconds-SPH-AAAAAA']").Input("60")
        );

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='add-exercise-submit-SPH-AAAAAA']").Click()
        );

        // Assert
        await _programExercisesApiClient
            .Received(1)
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            );
        Assert.NotEmpty(cut.FindAll("[data-testid='program-exercise-name-PGX-CCCCCC']"));
    }

    [Fact]
    public async Task AddProgramExercise_SubmitFailure_ShowsErrorScopedToPhase()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(
                new CreateProgramExerciseFailed("Could not add exercise to phase. Try again.")
            );
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']").Change("EXR-AAAAAA")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-count-SPH-AAAAAA']").Input("10")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-sets-SPH-AAAAAA']").Input("3")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-restseconds-SPH-AAAAAA']").Input("60")
        );

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='add-exercise-submit-SPH-AAAAAA']").Click()
        );

        // Assert
        Assert.Equal(
            "Could not add exercise to phase. Try again.",
            cut.Find("[data-testid='add-exercise-error-SPH-AAAAAA']").TextContent.Trim()
        );
    }

    [Fact]
    public async Task AddProgramExercise_EnterKeyWithValidFields_CallsCreateAndAppendsRow()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(
                new CreateProgramExerciseSucceeded(
                    new RepsProgramExercise(
                        ProgramExerciseId.Parse("PGX-CCCCCC"),
                        sessionPhaseId,
                        ExerciseId.Parse("EXR-AAAAAA"),
                        10,
                        0,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Both
                    )
                )
            );
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']").Change("EXR-AAAAAA")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-sets-SPH-AAAAAA']").Input("3")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-restseconds-SPH-AAAAAA']").Input("60")
        );
        var countInput = cut.Find("[data-testid='exercise-count-SPH-AAAAAA']");
        await cut.InvokeAsync(() => countInput.Input("10"));

        // Act
        await cut.InvokeAsync(() => countInput.KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        // Assert
        await _programExercisesApiClient
            .Received(1)
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            );
        Assert.NotEmpty(cut.FindAll("[data-testid='program-exercise-name-PGX-CCCCCC']"));
    }

    [Fact]
    public async Task AddProgramExercise_EnterKeyWithMissingFields_DoesNotCallApi()
    {
        // Arrange
        SetUpProgramSessionPhase();
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());
        var countInput = cut.Find("[data-testid='exercise-count-SPH-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => countInput.KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        // Assert
        await _programExercisesApiClient
            .DidNotReceive()
            .CreateRepsProgramExerciseAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<SessionPhaseId>(),
                Arg.Any<ExerciseId>(),
                Arg.Any<int>(),
                Arg.Any<SetPrescription>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task AddProgramExercise_EnterKeyRepeatedWhileCreateInFlight_CallsCreateOnlyOnce()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var tcs = new TaskCompletionSource<CreateProgramExerciseOutcome>();
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']").Change("EXR-AAAAAA")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-sets-SPH-AAAAAA']").Input("3")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-restseconds-SPH-AAAAAA']").Input("60")
        );
        var countInput = cut.Find("[data-testid='exercise-count-SPH-AAAAAA']");
        await cut.InvokeAsync(() => countInput.Input("10"));

        // Act
        var firstKeyDown = cut.InvokeAsync(() =>
            countInput.KeyDown(new KeyboardEventArgs { Key = "Enter" })
        );
        await cut.InvokeAsync(() => countInput.KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        tcs.SetResult(
            new CreateProgramExerciseSucceeded(
                new RepsProgramExercise(
                    ProgramExerciseId.Parse("PGX-CCCCCC"),
                    sessionPhaseId,
                    ExerciseId.Parse("EXR-AAAAAA"),
                    10,
                    0,
                    new SetPrescription(3, 60),
                    ProgramExerciseSide.Both
                )
            )
        );
        await firstKeyDown;

        // Assert
        await _programExercisesApiClient
            .Received(1)
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            );
    }

    [Fact]
    public async Task AddProgramExercise_CreateInFlightOnOnePhase_BlocksOpeningAnotherPhasesForm()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        var sessionPhaseA = new SessionPhaseSummary(
            SessionPhaseId.Parse("SPH-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            PhaseId.Parse("PHS-AAAAAA")
        );
        var sessionPhaseB = new SessionPhaseSummary(
            SessionPhaseId.Parse("SPH-BBBBBB"),
            SessionId.Parse("SNN-AAAAAA"),
            PhaseId.Parse("PHS-BBBBBB")
        );
        _treeApiClient
            .GetProgramTreeAsync(CancellationToken.None)
            .Returns([Tree(program, Tree(session, Tree(sessionPhaseA), Tree(sessionPhaseB)))]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([
                new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up"),
                new PhaseSummary(PhaseId.Parse("PHS-BBBBBB"), "Cool Down"),
            ]);
        _exercisesApiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns([new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat")]);
        var tcs = new TaskCompletionSource<CreateProgramExerciseOutcome>();
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                SessionPhaseId.Parse("SPH-AAAAAA"),
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']").Change("EXR-AAAAAA")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-sets-SPH-AAAAAA']").Input("3")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-restseconds-SPH-AAAAAA']").Input("60")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-count-SPH-AAAAAA']").Input("10")
        );

        // Act: submit phase A's form, leaving its create call pending, then try to
        // open phase B's add-exercise picker while phase A's is still in flight
        var createTask = cut.InvokeAsync(() =>
            cut.Find("[data-testid='add-exercise-submit-SPH-AAAAAA']").Click()
        );
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-BBBBBB']").Click());

        // Assert: phase B's form did not open -- the shared form fields are still
        // scoped to phase A's pending submission, so opening phase B would let its
        // completion clear them out from under phase A (or vice versa)
        Assert.Empty(cut.FindAll("[data-testid='exercise-picker-SPH-BBBBBB']"));
        Assert.True(cut.Find("[data-testid='add-exercise-SPH-BBBBBB']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").HasAttribute("disabled"));

        await cut.InvokeAsync(() =>
            tcs.SetResult(
                new CreateProgramExerciseSucceeded(
                    new RepsProgramExercise(
                        ProgramExerciseId.Parse("PGX-CCCCCC"),
                        SessionPhaseId.Parse("SPH-AAAAAA"),
                        ExerciseId.Parse("EXR-AAAAAA"),
                        10,
                        0,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Both
                    )
                )
            )
        );
        await createTask;
    }

    [Fact]
    public async Task AddProgramExercise_CreateInFlight_DisablesFormSoLaterEditsAreNotSilentlyLost()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var tcs = new TaskCompletionSource<CreateProgramExerciseOutcome>();
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click());
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']").Change("EXR-AAAAAA")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-sets-SPH-AAAAAA']").Input("3")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='exercise-restseconds-SPH-AAAAAA']").Input("60")
        );
        var countInput = cut.Find("[data-testid='exercise-count-SPH-AAAAAA']");
        await cut.InvokeAsync(() => countInput.Input("10"));

        // Act
        var createTask = cut.InvokeAsync(() =>
            countInput.KeyDown(new KeyboardEventArgs { Key = "Enter" })
        );

        // Assert
        Assert.True(cut.Find("[data-testid='exercise-count-SPH-AAAAAA']").HasAttribute("disabled"));
        Assert.True(
            cut.Find("[data-testid='add-exercise-submit-SPH-AAAAAA']").HasAttribute("disabled")
        );

        tcs.SetResult(
            new CreateProgramExerciseSucceeded(
                new RepsProgramExercise(
                    ProgramExerciseId.Parse("PGX-CCCCCC"),
                    sessionPhaseId,
                    ExerciseId.Parse("EXR-AAAAAA"),
                    10,
                    0,
                    new SetPrescription(3, 60),
                    ProgramExerciseSide.Both
                )
            )
        );
        await createTask;
    }

    [Fact]
    public void ProgramExerciseRow_WeightIsZero_RendersWeightAsEnDash()
    {
        // Arrange
        var (_, _, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Equal(
            "–",
            cut.Find("[data-testid='program-exercise-weight-PGX-AAAAAA']").GetAttribute("value")
        );
    }

    [Fact]
    public void ProgramExerciseRow_TimedType_RendersDurationInCountCellAndTimedBadge()
    {
        // Arrange
        var (_, _, sessionPhaseId) = SetUpProgramSessionPhase();
        var timed = new TimedProgramExercise(
            ProgramExerciseId.Parse("PGX-BBBBBB"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            30,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(timed);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Equal(
            "Timed",
            cut.Find("[data-testid='program-exercise-type-PGX-BBBBBB']").TextContent.Trim()
        );
        Assert.Equal(
            "30",
            cut.Find("[data-testid='program-exercise-count-PGX-BBBBBB']").GetAttribute("value")
        );
    }

    [Fact]
    public void ProgramExerciseRow_NonZeroValues_RendersActualNumbersWithoutDash()
    {
        // Arrange
        var (_, _, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Equal(
            "45",
            cut.Find("[data-testid='program-exercise-weight-PGX-AAAAAA']").GetAttribute("value")
        );
        Assert.Equal(
            "10",
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").GetAttribute("value")
        );
    }

    [Fact]
    public async Task ProgramExerciseRow_TypeSubOneWeightStartingFromDash_KeepsIntermediateKeystrokes()
    {
        // Arrange
        var (_, _, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        var cut = Render<Programs>();
        var weightInput = cut.Find("[data-testid='program-exercise-weight-PGX-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => weightInput.Input("0"));
        await cut.InvokeAsync(() => weightInput.Input("0."));
        await cut.InvokeAsync(() => weightInput.Input("0.5"));

        // Assert
        Assert.Equal(
            "0.5",
            cut.Find("[data-testid='program-exercise-weight-PGX-AAAAAA']").GetAttribute("value")
        );
    }

    [Fact]
    public async Task SaveProgramExercise_WeightIsUnparsableText_ShowsErrorAndDoesNotCallUpdate()
    {
        // Arrange
        var (_, _, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        var cut = Render<Programs>();
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-weight-PGX-AAAAAA']").Input("abc")
        );

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-save-PGX-AAAAAA']").Click()
        );

        // Assert
        await _programExercisesApiClient
            .DidNotReceive()
            .UpdateProgramExerciseAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<SessionPhaseId>(),
                Arg.Any<ProgramExerciseId>(),
                Arg.Any<ProgramExerciseUpdate>(),
                Arg.Any<CancellationToken>()
            );
        Assert.Equal(
            "Weight must be a valid number.",
            cut.Find("[data-testid='program-exercise-error-PGX-AAAAAA']").TextContent.Trim()
        );
    }

    [Fact]
    public async Task SaveProgramExercise_EditThenSave_CallsUpdateAndAppliesResult()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                Arg.Any<ProgramExerciseUpdate>(),
                CancellationToken.None
            )
            .Returns(
                new UpdateProgramExerciseSucceeded(
                    new RepsProgramExercise(
                        ProgramExerciseId.Parse("PGX-AAAAAA"),
                        sessionPhaseId,
                        ExerciseId.Parse("EXR-AAAAAA"),
                        12,
                        45,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Both
                    )
                )
            );
        var cut = Render<Programs>();
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").Input("12")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-weight-PGX-AAAAAA']").Input("45")
        );

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-save-PGX-AAAAAA']").Click()
        );

        // Assert
        Assert.Equal(
            "12",
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").GetAttribute("value")
        );
        Assert.Equal(
            "45",
            cut.Find("[data-testid='program-exercise-weight-PGX-AAAAAA']").GetAttribute("value")
        );
        Assert.Empty(cut.FindAll("[data-testid='program-exercise-save-PGX-AAAAAA']"));
    }

    [Fact]
    public async Task SaveProgramExercise_EnterKeyOnDirtyRow_CallsUpdateAndAppliesResult()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        var expectedUpdate = new ProgramExerciseUpdate(
            Reps: 12,
            Weight: 45,
            Sets: 3,
            RestSeconds: 60,
            Side: ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                expectedUpdate,
                CancellationToken.None
            )
            .Returns(
                new UpdateProgramExerciseSucceeded(
                    new RepsProgramExercise(
                        ProgramExerciseId.Parse("PGX-AAAAAA"),
                        sessionPhaseId,
                        ExerciseId.Parse("EXR-AAAAAA"),
                        12,
                        45,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Both
                    )
                )
            );
        var cut = Render<Programs>();
        var countInput = cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']");
        await cut.InvokeAsync(() => countInput.Input("12"));
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-weight-PGX-AAAAAA']").Input("45")
        );

        // Act
        await cut.InvokeAsync(() => countInput.KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        // Assert
        await _programExercisesApiClient
            .Received(1)
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                expectedUpdate,
                CancellationToken.None
            );
        Assert.Equal(
            "12",
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").GetAttribute("value")
        );
        Assert.Equal(
            "45",
            cut.Find("[data-testid='program-exercise-weight-PGX-AAAAAA']").GetAttribute("value")
        );
        Assert.Empty(cut.FindAll("[data-testid='program-exercise-save-PGX-AAAAAA']"));
    }

    [Fact]
    public async Task SaveProgramExercise_EnterKeyOnCleanRow_DoesNotCallApi()
    {
        // Arrange
        var (_, _, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        var cut = Render<Programs>();
        var countInput = cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => countInput.KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        // Assert
        await _programExercisesApiClient
            .DidNotReceive()
            .UpdateProgramExerciseAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<SessionPhaseId>(),
                Arg.Any<ProgramExerciseId>(),
                Arg.Any<ProgramExerciseUpdate>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task SaveProgramExercise_EnterKeyRepeatedWhileSaveInFlight_CallsUpdateOnlyOnce()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        var tcs = new TaskCompletionSource<UpdateProgramExerciseOutcome>();
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                Arg.Any<ProgramExerciseUpdate>(),
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();
        var countInput = cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']");
        await cut.InvokeAsync(() => countInput.Input("12"));

        // Act
        var firstKeyDown = cut.InvokeAsync(() =>
            countInput.KeyDown(new KeyboardEventArgs { Key = "Enter" })
        );
        await cut.InvokeAsync(() => countInput.KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        tcs.SetResult(
            new UpdateProgramExerciseSucceeded(
                new RepsProgramExercise(
                    ProgramExerciseId.Parse("PGX-AAAAAA"),
                    sessionPhaseId,
                    ExerciseId.Parse("EXR-AAAAAA"),
                    12,
                    0,
                    new SetPrescription(3, 60),
                    ProgramExerciseSide.Both
                )
            )
        );
        await firstKeyDown;

        // Assert
        await _programExercisesApiClient
            .Received(1)
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                Arg.Any<ProgramExerciseUpdate>(),
                CancellationToken.None
            );
    }

    [Fact]
    public async Task SaveProgramExercise_SaveInFlight_DisablesRowSoLaterEditsAreNotSilentlyLost()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        var tcs = new TaskCompletionSource<UpdateProgramExerciseOutcome>();
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                Arg.Any<ProgramExerciseUpdate>(),
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();
        var countInput = cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']");
        await cut.InvokeAsync(() => countInput.Input("12"));

        // Act
        var saveTask = cut.InvokeAsync(() =>
            countInput.KeyDown(new KeyboardEventArgs { Key = "Enter" })
        );

        // Assert
        Assert.True(
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").HasAttribute("disabled")
        );

        await cut.InvokeAsync(() =>
            tcs.SetResult(
                new UpdateProgramExerciseSucceeded(
                    new RepsProgramExercise(
                        ProgramExerciseId.Parse("PGX-AAAAAA"),
                        sessionPhaseId,
                        ExerciseId.Parse("EXR-AAAAAA"),
                        12,
                        0,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Both
                    )
                )
            )
        );
        await saveTask;
        Assert.False(
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").HasAttribute("disabled")
        );
    }

    [Fact]
    public async Task SaveProgramExercise_NonPositiveCount_ShowsValidationErrorWithoutApiCall()
    {
        // Arrange
        var (_, _, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        var cut = Render<Programs>();
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").Input("0")
        );

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-save-PGX-AAAAAA']").Click()
        );

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='program-exercise-error-PGX-AAAAAA']"));
        await _programExercisesApiClient
            .DidNotReceive()
            .UpdateProgramExerciseAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<SessionPhaseId>(),
                Arg.Any<ProgramExerciseId>(),
                Arg.Any<ProgramExerciseUpdate>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task SaveProgramExercise_ServerRejects_ShowsErrorOnRowWithoutThrowing()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                Arg.Any<ProgramExerciseUpdate>(),
                CancellationToken.None
            )
            .Returns(new UpdateProgramExerciseFailed("Request failed with status 500."));
        var cut = Render<Programs>();
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").Input("12")
        );

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-save-PGX-AAAAAA']").Click()
        );

        // Assert
        Assert.Equal(
            "Request failed with status 500.",
            cut.Find("[data-testid='program-exercise-error-PGX-AAAAAA']").TextContent.Trim()
        );
    }

    [Fact]
    public async Task RevertProgramExercise_ClickRevert_RestoresSavedValuesAndMakesNoApiCall()
    {
        // Arrange
        var (_, _, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        var cut = Render<Programs>();
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").Input("99")
        );

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-revert-PGX-AAAAAA']").Click()
        );

        // Assert
        Assert.Equal(
            "10",
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").GetAttribute("value")
        );
        await _programExercisesApiClient
            .DidNotReceive()
            .UpdateProgramExerciseAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<SessionPhaseId>(),
                Arg.Any<ProgramExerciseId>(),
                Arg.Any<ProgramExerciseUpdate>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task DeleteProgramExercise_ClickDelete_CallsDeleteAndRemovesRow()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        _programExercisesApiClient
            .DeleteProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                CancellationToken.None
            )
            .Returns(new DeleteProgramExerciseSucceeded());
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-delete-PGX-AAAAAA']").Click()
        );

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='program-exercise-name-PGX-AAAAAA']"));
    }

    [Fact]
    public async Task DeleteProgramExercise_ServerRejects_ShowsErrorOnRowWithoutRemovingIt()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        _programExercisesApiClient
            .DeleteProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                CancellationToken.None
            )
            .Returns(new DeleteProgramExerciseFailed("Request failed with status 500."));
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-delete-PGX-AAAAAA']").Click()
        );

        // Assert
        Assert.Equal(
            "Request failed with status 500.",
            cut.Find("[data-testid='program-exercise-error-PGX-AAAAAA']").TextContent.Trim()
        );
        Assert.NotEmpty(cut.FindAll("[data-testid='program-exercise-name-PGX-AAAAAA']"));
    }

    [Fact]
    public void DuplicateButton_ProgramExerciseRow_IsRendered()
    {
        // Arrange
        var (_, _, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']"));
    }

    [Fact]
    public async Task DuplicateProgramExercise_ClickDuplicateOnRepsRow_CallsCreateThenUpdateWithSourceValues()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Left
        );
        SetProgramExercises(reps);
        var created = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-CCCCCC"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(new CreateProgramExerciseSucceeded(created));
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-CCCCCC"),
                new ProgramExerciseUpdate(
                    Reps: 10,
                    Weight: 45,
                    Sets: 3,
                    RestSeconds: 60,
                    Side: ProgramExerciseSide.Left
                ),
                CancellationToken.None
            )
            .Returns(
                new UpdateProgramExerciseSucceeded(
                    new RepsProgramExercise(
                        ProgramExerciseId.Parse("PGX-CCCCCC"),
                        sessionPhaseId,
                        ExerciseId.Parse("EXR-AAAAAA"),
                        10,
                        45,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Left
                    )
                )
            );
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );

        // Assert
        await _programExercisesApiClient
            .Received(1)
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            );
        await _programExercisesApiClient
            .Received(1)
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-CCCCCC"),
                new ProgramExerciseUpdate(
                    Reps: 10,
                    Weight: 45,
                    Sets: 3,
                    RestSeconds: 60,
                    Side: ProgramExerciseSide.Left
                ),
                CancellationToken.None
            );
    }

    [Fact]
    public async Task DuplicateProgramExercise_ClickDuplicateOnTimedRow_CallsCreateTimedThenUpdateWithSourceValues()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var timed = new TimedProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            30,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Right
        );
        SetProgramExercises(timed);
        var created = new TimedProgramExercise(
            ProgramExerciseId.Parse("PGX-CCCCCC"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            30,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .CreateTimedProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                30,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(new CreateProgramExerciseSucceeded(created));
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-CCCCCC"),
                new ProgramExerciseUpdate(
                    DurationSeconds: 30,
                    Weight: 45,
                    Sets: 3,
                    RestSeconds: 60,
                    Side: ProgramExerciseSide.Right
                ),
                CancellationToken.None
            )
            .Returns(
                new UpdateProgramExerciseSucceeded(
                    new TimedProgramExercise(
                        ProgramExerciseId.Parse("PGX-CCCCCC"),
                        sessionPhaseId,
                        ExerciseId.Parse("EXR-AAAAAA"),
                        30,
                        45,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Right
                    )
                )
            );
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );

        // Assert
        await _programExercisesApiClient
            .Received(1)
            .CreateTimedProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                30,
                new SetPrescription(3, 60),
                CancellationToken.None
            );
        await _programExercisesApiClient
            .Received(1)
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-CCCCCC"),
                new ProgramExerciseUpdate(
                    DurationSeconds: 30,
                    Weight: 45,
                    Sets: 3,
                    RestSeconds: 60,
                    Side: ProgramExerciseSide.Right
                ),
                CancellationToken.None
            );
    }

    [Fact]
    public async Task DuplicateProgramExercise_TwoExistingRows_NewRowAppendedAtEndNotNextToSource()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var first = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        var second = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-BBBBBB"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            8,
            20,
            new SetPrescription(4, 30),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(first, second);
        var created = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-CCCCCC"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(new CreateProgramExerciseSucceeded(created));
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-CCCCCC"),
                Arg.Any<ProgramExerciseUpdate>(),
                CancellationToken.None
            )
            .Returns(
                new UpdateProgramExerciseSucceeded(
                    new RepsProgramExercise(
                        ProgramExerciseId.Parse("PGX-CCCCCC"),
                        sessionPhaseId,
                        ExerciseId.Parse("EXR-AAAAAA"),
                        10,
                        45,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Both
                    )
                )
            );
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );

        // Assert
        var rows = cut.FindAll("[data-testid^='program-exercise-name-']");
        Assert.Equal(3, rows.Count);
        Assert.Equal("program-exercise-name-PGX-CCCCCC", rows[2].GetAttribute("data-testid"));
    }

    [Fact]
    public async Task DuplicateProgramExercise_SourceRowIsDirty_ClonesLastSavedValuesNotWorkingValues()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        var created = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-CCCCCC"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(new CreateProgramExerciseSucceeded(created));
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-CCCCCC"),
                new ProgramExerciseUpdate(
                    Reps: 10,
                    Weight: 45,
                    Sets: 3,
                    RestSeconds: 60,
                    Side: ProgramExerciseSide.Both
                ),
                CancellationToken.None
            )
            .Returns(
                new UpdateProgramExerciseSucceeded(
                    new RepsProgramExercise(
                        ProgramExerciseId.Parse("PGX-CCCCCC"),
                        sessionPhaseId,
                        ExerciseId.Parse("EXR-AAAAAA"),
                        10,
                        45,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Both
                    )
                )
            );
        var cut = Render<Programs>();
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").Input("99")
        );

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );

        // Assert
        await _programExercisesApiClient
            .Received(1)
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            );
        Assert.Equal(
            "99",
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").GetAttribute("value")
        );
    }

    [Fact]
    public async Task DuplicateProgramExercise_CreateFails_ShowsErrorOnSourceRowMakesNoUpdateCallAndAddsNoRow()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(
                new CreateProgramExerciseFailed("Could not add exercise to phase. Try again.")
            );
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );

        // Assert
        Assert.Equal(
            "Could not add exercise to phase. Try again.",
            cut.Find("[data-testid='program-exercise-error-PGX-AAAAAA']").TextContent.Trim()
        );
        await _programExercisesApiClient
            .DidNotReceive()
            .UpdateProgramExerciseAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<SessionPhaseId>(),
                Arg.Any<ProgramExerciseId>(),
                Arg.Any<ProgramExerciseUpdate>(),
                Arg.Any<CancellationToken>()
            );
        Assert.Single(cut.FindAll("[data-testid^='program-exercise-name-']"));
    }

    [Fact]
    public async Task DuplicateProgramExercise_UpdateFails_AppendsRowWithDefaultsAndShowsErrorOnNewRow()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Left
        );
        SetProgramExercises(reps);
        var created = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-CCCCCC"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(new CreateProgramExerciseSucceeded(created));
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-CCCCCC"),
                Arg.Any<ProgramExerciseUpdate>(),
                CancellationToken.None
            )
            .Returns(new UpdateProgramExerciseFailed("Request failed with status 500."));
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='program-exercise-error-PGX-AAAAAA']"));
        Assert.Equal(
            "Request failed with status 500.",
            cut.Find("[data-testid='program-exercise-error-PGX-CCCCCC']").TextContent.Trim()
        );
        Assert.Equal(
            "–",
            cut.Find("[data-testid='program-exercise-weight-PGX-CCCCCC']").GetAttribute("value")
        );
        var sideSelect = cut.Find("[data-testid='program-exercise-side-PGX-CCCCCC']");
        var selectedOption = sideSelect.Children.Single(option => option.HasAttribute("selected"));
        Assert.Equal("Both", selectedOption.TextContent);
    }

    [Fact]
    public async Task DuplicateProgramExercise_SourceRowHasDefaultWeightAndSide_StillIssuesUpdateCall()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        var created = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-CCCCCC"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(new CreateProgramExerciseSucceeded(created));
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-CCCCCC"),
                new ProgramExerciseUpdate(
                    Reps: 10,
                    Weight: 0,
                    Sets: 3,
                    RestSeconds: 60,
                    Side: ProgramExerciseSide.Both
                ),
                CancellationToken.None
            )
            .Returns(
                new UpdateProgramExerciseSucceeded(
                    new RepsProgramExercise(
                        ProgramExerciseId.Parse("PGX-CCCCCC"),
                        sessionPhaseId,
                        ExerciseId.Parse("EXR-AAAAAA"),
                        10,
                        0,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Both
                    )
                )
            );
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );

        // Assert
        await _programExercisesApiClient
            .Received(1)
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-CCCCCC"),
                new ProgramExerciseUpdate(
                    Reps: 10,
                    Weight: 0,
                    Sets: 3,
                    RestSeconds: 60,
                    Side: ProgramExerciseSide.Both
                ),
                CancellationToken.None
            );
    }

    [Fact]
    public async Task DuplicateProgramExercise_DoubleClickWhileCreateInFlight_CallsCreateOnlyOnce()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Left
        );
        SetProgramExercises(reps);
        var tcs = new TaskCompletionSource<CreateProgramExerciseOutcome>();
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();

        // Act
        var firstClick = cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );
        tcs.SetResult(
            new CreateProgramExerciseSucceeded(
                new RepsProgramExercise(
                    ProgramExerciseId.Parse("PGX-CCCCCC"),
                    sessionPhaseId,
                    ExerciseId.Parse("EXR-AAAAAA"),
                    10,
                    0,
                    new SetPrescription(3, 60),
                    ProgramExerciseSide.Both
                )
            )
        );
        await firstClick;

        // Assert
        await _programExercisesApiClient
            .Received(1)
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            );
    }

    [Fact]
    public async Task DuplicateProgramExercise_UpdateInFlight_NewRowDuplicateButtonDisabled()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Left
        );
        SetProgramExercises(reps);
        var created = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-CCCCCC"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(new CreateProgramExerciseSucceeded(created));
        var tcs = new TaskCompletionSource<UpdateProgramExerciseOutcome>();
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-CCCCCC"),
                Arg.Any<ProgramExerciseUpdate>(),
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();

        // Act
        var duplicateTask = cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );

        // Assert
        Assert.True(
            cut.Find("[data-testid='program-exercise-duplicate-PGX-CCCCCC']")
                .HasAttribute("disabled")
        );

        await cut.InvokeAsync(() =>
            tcs.SetResult(
                new UpdateProgramExerciseSucceeded(
                    new RepsProgramExercise(
                        ProgramExerciseId.Parse("PGX-CCCCCC"),
                        sessionPhaseId,
                        ExerciseId.Parse("EXR-AAAAAA"),
                        10,
                        45,
                        new SetPrescription(3, 60),
                        ProgramExerciseSide.Left
                    )
                )
            )
        );
        await duplicateTask;
        Assert.False(
            cut.Find("[data-testid='program-exercise-duplicate-PGX-CCCCCC']")
                .HasAttribute("disabled")
        );
    }

    [Fact]
    public async Task DuplicateProgramExercise_RetryAfterCreateFailed_ClearsStaleSourceErrorOnceCreateSucceeds()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Left
        );
        SetProgramExercises(reps);
        var created = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-CCCCCC"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .CreateRepsProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                new SetPrescription(3, 60),
                CancellationToken.None
            )
            .Returns(
                new CreateProgramExerciseFailed("Could not add exercise to phase. Try again."),
                new CreateProgramExerciseSucceeded(created)
            );
        _programExercisesApiClient
            .UpdateProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-CCCCCC"),
                Arg.Any<ProgramExerciseUpdate>(),
                CancellationToken.None
            )
            .Returns(new UpdateProgramExerciseFailed("Request failed with status 500."));
        var cut = Render<Programs>();
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );
        Assert.Equal(
            "Could not add exercise to phase. Try again.",
            cut.Find("[data-testid='program-exercise-error-PGX-AAAAAA']").TextContent.Trim()
        );

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click()
        );

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='program-exercise-error-PGX-AAAAAA']"));
        Assert.Equal(
            "Request failed with status 500.",
            cut.Find("[data-testid='program-exercise-error-PGX-CCCCCC']").TextContent.Trim()
        );
    }

    [Fact]
    public async Task DeleteProgramExercise_DoubleClickWhileDeleteInFlight_CallsDeleteOnlyOnce()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            new SetPrescription(3, 60),
            ProgramExerciseSide.Both
        );
        SetProgramExercises(reps);
        var tcs = new TaskCompletionSource<DeleteProgramExerciseOutcome>();
        _programExercisesApiClient
            .DeleteProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Programs>();

        // Act
        var firstClick = cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-delete-PGX-AAAAAA']").Click()
        );

        // Assert
        Assert.True(
            cut.Find("[data-testid='program-exercise-delete-PGX-AAAAAA']").HasAttribute("disabled")
        );
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='program-exercise-delete-PGX-AAAAAA']").Click()
        );
        tcs.SetResult(new DeleteProgramExerciseSucceeded());
        await firstClick;

        await _programExercisesApiClient
            .Received(1)
            .DeleteProgramExerciseAsync(
                programId,
                sessionId,
                sessionPhaseId,
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                CancellationToken.None
            );
    }
}
