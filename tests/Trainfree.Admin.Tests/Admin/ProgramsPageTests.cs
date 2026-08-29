using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Pages.Admin;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Admin;

public sealed class ProgramsPageTests : BunitContext
{
    private readonly IProgramsApiClient _apiClient = Substitute.For<IProgramsApiClient>();
    private readonly ISessionsApiClient _sessionsApiClient = Substitute.For<ISessionsApiClient>();

    public ProgramsPageTests()
    {
        Services.AddSingleton(_apiClient);
        Services.AddSingleton(_sessionsApiClient);
        _sessionsApiClient
            .GetSessionsAsync(Arg.Any<ProgramId>(), Arg.Any<CancellationToken>())
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
}
