using System.Text.Json;
using Bunit;
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
    private readonly ISessionsApiClient _sessionsApiClient = Substitute.For<ISessionsApiClient>();
    private readonly IPhasesApiClient _phasesApiClient = Substitute.For<IPhasesApiClient>();
    private readonly ISessionPhasesApiClient _sessionPhasesApiClient =
        Substitute.For<ISessionPhasesApiClient>();
    private readonly IExercisesApiClient _exercisesApiClient =
        Substitute.For<IExercisesApiClient>();
    private readonly IProgramExercisesApiClient _programExercisesApiClient =
        Substitute.For<IProgramExercisesApiClient>();

    public ProgramsPageTests()
    {
        Services.AddSingleton(_apiClient);
        Services.AddSingleton(_sessionsApiClient);
        Services.AddSingleton(_phasesApiClient);
        Services.AddSingleton(_sessionPhasesApiClient);
        Services.AddSingleton(_exercisesApiClient);
        Services.AddSingleton(_programExercisesApiClient);
        _sessionsApiClient
            .GetSessionsAsync(Arg.Any<ProgramId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _phasesApiClient.GetPhasesAsync(Arg.Any<CancellationToken>()).Returns([]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([]);
        _exercisesApiClient.GetExercisesAsync(Arg.Any<CancellationToken>()).Returns([]);
        _programExercisesApiClient
            .GetProgramExercisesAsync(
                Arg.Any<ProgramId>(),
                Arg.Any<SessionId>(),
                Arg.Any<SessionPhaseId>(),
                Arg.Any<CancellationToken>()
            )
            .Returns([]);
    }

    [Fact]
    public void OnInitialized_ServerReturnsTheAccessLoginPage_ShowsTheLoadErrorInsteadOfFailing()
    {
        // Arrange
        _apiClient
            .GetProgramsAsync(CancellationToken.None)
            .Returns<IReadOnlyList<ProgramSummary>>(_ =>
                throw new JsonException("'<' is an invalid start of a value.")
            );

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid=load-programs-error]"));
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_OneProgramSessionsFailToLoad_StillRendersEveryProgramRow()
    {
        // Arrange
        _apiClient
            .GetProgramsAsync(CancellationToken.None)
            .Returns([
                new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A"),
                new ProgramSummary(ProgramId.Parse("PRG-BBBBBB"), "Workout B"),
            ]);
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns<IReadOnlyList<SessionSummary>>(_ =>
                throw new JsonException("'<' is an invalid start of a value.")
            );

        // Act
        var cut = Render<Programs>();

        // Assert
        var rows = cut.FindAll("tbody tr");
        Assert.Contains("Workout A", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Workout B", cut.Markup, StringComparison.Ordinal);
        Assert.NotEmpty(rows);
        Assert.NotNull(cut.Find("[data-testid=sessions-load-error-PRG-AAAAAA]"));
    }

    [Fact]
    public void OnInitialized_ExistingPrograms_RendersOneRowPerProgram()
    {
        // Arrange
        _apiClient
            .GetProgramsAsync(CancellationToken.None)
            .Returns([
                new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A"),
                new ProgramSummary(ProgramId.Parse("PRG-BBBBBB"), "Workout B"),
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([]);
        var created = new ProgramSummary(ProgramId.Parse("PRG-CCCCCC"), "New Program");
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var renamed = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Renamed Workout");
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
    public void SaveButton_NoUnsavedChanges_IsNotShown()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);

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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);

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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
        _apiClient
            .GetProgramsAsync(CancellationToken.None)
            .Returns<Task<IReadOnlyList<ProgramSummary>>>(_ =>
                throw new HttpRequestException("simulated failure")
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([
                new SessionSummary(
                    SessionId.Parse("SNN-AAAAAA"),
                    ProgramId.Parse("PRG-AAAAAA"),
                    "Monday Lower Body"
                ),
                new SessionSummary(
                    SessionId.Parse("SNN-BBBBBB"),
                    ProgramId.Parse("PRG-AAAAAA"),
                    "Wednesday Upper Body"
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var created = new SessionSummary(
            SessionId.Parse("SNN-CCCCCC"),
            ProgramId.Parse("PRG-AAAAAA"),
            "New Session"
        );
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var created = new SessionSummary(
            SessionId.Parse("SNN-CCCCCC"),
            ProgramId.Parse("PRG-AAAAAA"),
            "New Session"
        );
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
        var renamed = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Renamed Session"
        );
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
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
    public async Task RevertSession_ClickRevert_RestoresOriginalNameAndHidesButtons()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);

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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='chevron-PRG-AAAAAA']").Click());

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='chevron-PRG-AAAAAA']").Click());

        // Assert
        Assert.Contains("Monday Lower Body", cut.Markup, StringComparison.Ordinal);
        await _sessionsApiClient
            .Received(1)
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None);
    }

    [Fact]
    public async Task Chevron_CollapseOneProgram_LeavesOtherProgramsExpanded()
    {
        // Arrange
        var programA = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        var programB = new ProgramSummary(ProgramId.Parse("PRG-BBBBBB"), "Workout B");
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([programA, programB]);
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([
                new SessionSummary(
                    SessionId.Parse("SNN-AAAAAA"),
                    ProgramId.Parse("PRG-AAAAAA"),
                    "Monday Lower Body"
                ),
            ]);
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-BBBBBB"), CancellationToken.None)
            .Returns([
                new SessionSummary(
                    SessionId.Parse("SNN-BBBBBB"),
                    ProgramId.Parse("PRG-BBBBBB"),
                    "Tuesday Upper Body"
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
    public async Task Chevron_ClickOnProgramWhoseSessionsFailedToLoad_TogglesErrorPanel()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns<IReadOnlyList<SessionSummary>>(_ =>
                throw new JsonException("'<' is an invalid start of a value.")
            );
        var cut = Render<Programs>();
        var chevron = cut.Find("[data-testid='chevron-PRG-AAAAAA']");
        Assert.False(chevron.HasAttribute("disabled"));
        Assert.Equal("true", chevron.GetAttribute("aria-expanded"));
        Assert.Equal("Collapse", chevron.GetAttribute("title"));
        Assert.NotNull(cut.Find("[data-testid=sessions-load-error-PRG-AAAAAA]"));

        // Act
        await cut.InvokeAsync(() => chevron.Click());

        // Assert
        chevron = cut.Find("[data-testid='chevron-PRG-AAAAAA']");
        Assert.Equal("false", chevron.GetAttribute("aria-expanded"));
        Assert.Equal("Expand", chevron.GetAttribute("title"));
        Assert.Contains("chevron-collapsed", chevron.InnerHtml, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid=sessions-load-error-PRG-AAAAAA]"));
    }

    [Fact]
    public async Task DeleteSession_ServerRejects_ShowsErrorAndKeepsRowWithoutThrowing()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns([
                new SessionPhaseSummary(
                    SessionPhaseId.Parse("SPH-AAAAAA"),
                    SessionId.Parse("SNN-AAAAAA"),
                    PhaseId.Parse("PHS-AAAAAA")
                ),
            ]);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Contains("Warm Up", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void OnInitialized_OneSessionsPhasesFailToLoad_StillRendersEveryOtherRow()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([sessionA, sessionB]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns<IReadOnlyList<SessionPhaseSummary>>(_ =>
                throw new JsonException("'<' is an invalid start of a value.")
            );

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Contains("Monday Lower Body", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Wednesday Upper Body", cut.Markup, StringComparison.Ordinal);
        Assert.NotNull(cut.Find("[data-testid=phases-load-error-SNN-AAAAAA]"));
    }

    [Fact]
    public void OnInitialized_SessionHasPhases_StartsExpanded()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns([
                new SessionPhaseSummary(
                    SessionPhaseId.Parse("SPH-AAAAAA"),
                    SessionId.Parse("SNN-AAAAAA"),
                    PhaseId.Parse("PHS-AAAAAA")
                ),
            ]);

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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns([
                new SessionPhaseSummary(
                    SessionPhaseId.Parse("SPH-AAAAAA"),
                    SessionId.Parse("SNN-AAAAAA"),
                    PhaseId.Parse("PHS-AAAAAA")
                ),
            ]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns([
                new SessionPhaseSummary(
                    SessionPhaseId.Parse("SPH-AAAAAA"),
                    SessionId.Parse("SNN-AAAAAA"),
                    PhaseId.Parse("PHS-AAAAAA")
                ),
            ]);
        var cut = Render<Programs>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-chevron-SNN-AAAAAA']").Click());

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='session-chevron-SNN-AAAAAA']").Click());

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='phase-name-SPH-AAAAAA']"));
        await _sessionPhasesApiClient
            .Received(1)
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            );
    }

    [Fact]
    public async Task SessionChevron_CollapseOneSession_LeavesOtherSessionsExpanded()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
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
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([sessionA, sessionB]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns([
                new SessionPhaseSummary(
                    SessionPhaseId.Parse("SPH-AAAAAA"),
                    SessionId.Parse("SNN-AAAAAA"),
                    PhaseId.Parse("PHS-AAAAAA")
                ),
            ]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-BBBBBB"),
                CancellationToken.None
            )
            .Returns([
                new SessionPhaseSummary(
                    SessionPhaseId.Parse("SPH-BBBBBB"),
                    SessionId.Parse("SNN-BBBBBB"),
                    PhaseId.Parse("PHS-AAAAAA")
                ),
            ]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
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
    public async Task AddPhase_SelectPhase_CallsCreateAndAppendsRow()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns([
                new SessionPhaseSummary(
                    SessionPhaseId.Parse("SPH-AAAAAA"),
                    SessionId.Parse("SNN-AAAAAA"),
                    PhaseId.Parse("PHS-AAAAAA")
                ),
            ]);
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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns([
                new SessionPhaseSummary(
                    SessionPhaseId.Parse("SPH-AAAAAA"),
                    SessionId.Parse("SNN-AAAAAA"),
                    PhaseId.Parse("PHS-AAAAAA")
                ),
            ]);
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
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns([
                new SessionPhaseSummary(
                    SessionPhaseId.Parse("SPH-AAAAAA"),
                    SessionId.Parse("SNN-AAAAAA"),
                    PhaseId.Parse("PHS-AAAAAA")
                ),
            ]);
        _exercisesApiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns([new ExerciseSummary(ExerciseId.Parse(exerciseId), exerciseName)]);

        return (
            ProgramId.Parse("PRG-AAAAAA"),
            SessionId.Parse("SNN-AAAAAA"),
            SessionPhaseId.Parse("SPH-AAAAAA")
        );
    }

    [Fact]
    public void ProgramExercises_SessionPhaseHasProgramExercises_RendersRowsNestedUnderPhase()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([reps]);

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.Equal(
            "Bodyweight Squat",
            cut.Find("[data-testid='program-exercise-name-PGX-AAAAAA']").TextContent.Trim()
        );
    }

    [Fact]
    public void ProgramExercises_LoadFailsForOneSessionPhase_ShowsErrorButPhaseRowStillRenders()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns<Task<IReadOnlyList<IProgramExercise>>>(_ =>
                throw new HttpRequestException("simulated failure")
            );

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='program-exercises-load-error-SPH-AAAAAA']"));
        Assert.NotEmpty(cut.FindAll("[data-testid='phase-name-SPH-AAAAAA']"));
    }

    [Fact]
    public void ProgramExercises_LoadThrowsFormatException_ShowsErrorButPhaseRowStillRenders()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns<Task<IReadOnlyList<IProgramExercise>>>(_ =>
                throw new FormatException("Unknown program exercise type: 'Bogus'.")
            );

        // Act
        var cut = Render<Programs>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='program-exercises-load-error-SPH-AAAAAA']"));
        Assert.NotEmpty(cut.FindAll("[data-testid='phase-name-SPH-AAAAAA']"));
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
    public async Task AddProgramExercise_EmptyExerciseLibrary_ShowsGuidanceAndMakesNoApiCall()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns([program]);
        var session = new SessionSummary(
            SessionId.Parse("SNN-AAAAAA"),
            ProgramId.Parse("PRG-AAAAAA"),
            "Monday Lower Body"
        );
        _sessionsApiClient
            .GetSessionsAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns([session]);
        _phasesApiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up")]);
        _sessionPhasesApiClient
            .GetSessionPhasesAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                SessionId.Parse("SNN-AAAAAA"),
                CancellationToken.None
            )
            .Returns([
                new SessionPhaseSummary(
                    SessionPhaseId.Parse("SPH-AAAAAA"),
                    SessionId.Parse("SNN-AAAAAA"),
                    PhaseId.Parse("PHS-AAAAAA")
                ),
            ]);
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
                Arg.Any<int>(),
                Arg.Any<int>(),
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
                3,
                60,
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
                        3,
                        60,
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
                3,
                60,
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
                3,
                60,
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
    public void ProgramExerciseRow_WeightIsZero_RendersWeightAsEnDash()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([reps]);

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
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var timed = new TimedProgramExercise(
            ProgramExerciseId.Parse("PGX-BBBBBB"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            30,
            0,
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([timed]);

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
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([reps]);

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
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([reps]);
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
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            45,
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([reps]);
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
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([reps]);
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
                        3,
                        60,
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
    public async Task SaveProgramExercise_NonPositiveCount_ShowsValidationErrorWithoutApiCall()
    {
        // Arrange
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([reps]);
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
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([reps]);
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
        var (programId, sessionId, sessionPhaseId) = SetUpProgramSessionPhase();
        var reps = new RepsProgramExercise(
            ProgramExerciseId.Parse("PGX-AAAAAA"),
            sessionPhaseId,
            ExerciseId.Parse("EXR-AAAAAA"),
            10,
            0,
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([reps]);
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
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([reps]);
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
            3,
            60,
            ProgramExerciseSide.Both
        );
        _programExercisesApiClient
            .GetProgramExercisesAsync(programId, sessionId, sessionPhaseId, CancellationToken.None)
            .Returns([reps]);
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
}
