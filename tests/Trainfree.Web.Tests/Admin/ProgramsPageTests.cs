using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Trainfree.Web.Admin;
using Trainfree.Web.Ids;
using Trainfree.Web.Pages.Admin;

namespace Trainfree.Web.Tests.Admin;

public sealed class ProgramsPageTests : BunitContext
{
    private readonly IProgramsApiClient _apiClient = Substitute.For<IProgramsApiClient>();

    public ProgramsPageTests() => Services.AddSingleton(_apiClient);

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
            .Returns(new RenameProgramFailed("name must be between 5 and 100 characters"));
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Renamed Workout"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-PRG-AAAAAA']").Click());

        // Assert
        Assert.Contains(
            "name must be between 5 and 100 characters",
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
}
