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
            .Returns(
                new List<ProgramSummary>
                {
                    new(ProgramId.Parse("PRG-AAAAAA"), "Workout A"),
                    new(ProgramId.Parse("PRG-BBBBBB"), "Workout B"),
                }
            );

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
        _apiClient.GetProgramsAsync(CancellationToken.None).Returns(new List<ProgramSummary>());
        var created = new ProgramSummary(ProgramId.Parse("PRG-CCCCCC"), "New Program");
        _apiClient.CreateProgramAsync("New Program", CancellationToken.None).Returns(created);
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-program']").Click());

        // Assert
        await _apiClient.Received(1).CreateProgramAsync("New Program", CancellationToken.None);
        var input = cut.Find("[data-testid='name-input-PRG-CCCCCC']");
        Assert.True(input.HasAttribute("autofocus"));
    }

    [Fact]
    public async Task RenameProgram_NameBlurred_CallsRenameAndUpdatesDisplayedName()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient
            .GetProgramsAsync(CancellationToken.None)
            .Returns(new List<ProgramSummary> { program });
        var renamed = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Renamed Workout");
        _apiClient
            .RenameProgramAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "Renamed Workout",
                CancellationToken.None
            )
            .Returns(renamed);
        var cut = Render<Programs>();
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Change("Renamed Workout"));
        await cut.InvokeAsync(() => input.Blur());

        // Assert
        await _apiClient
            .Received(1)
            .RenameProgramAsync(
                ProgramId.Parse("PRG-AAAAAA"),
                "Renamed Workout",
                CancellationToken.None
            );
        Assert.Contains("Renamed Workout", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteProgram_ClickX_CallsDeleteAndRemovesRow()
    {
        // Arrange
        var program = new ProgramSummary(ProgramId.Parse("PRG-AAAAAA"), "Workout A");
        _apiClient
            .GetProgramsAsync(CancellationToken.None)
            .Returns(new List<ProgramSummary> { program });
        _apiClient
            .DeleteProgramAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None)
            .Returns(Task.CompletedTask);
        var cut = Render<Programs>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-PRG-AAAAAA']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .DeleteProgramAsync(ProgramId.Parse("PRG-AAAAAA"), CancellationToken.None);
        Assert.Empty(cut.FindAll("tbody tr"));
    }
}
