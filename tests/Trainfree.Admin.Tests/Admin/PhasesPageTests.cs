using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Pages;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Admin;

public sealed class PhasesPageTests : BunitContext
{
    private readonly IPhasesApiClient _apiClient = Substitute.For<IPhasesApiClient>();

    public PhasesPageTests() => Services.AddSingleton(_apiClient);

    [Fact]
    public void OnInitialized_ServerReturnsTheAccessLoginPage_ShowsTheLoadErrorInsteadOfFailing()
    {
        // Arrange
        _apiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns<IReadOnlyList<PhaseSummary>>(_ =>
                throw new JsonException("'<' is an invalid start of a value.")
            );

        // Act
        var cut = Render<Phases>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid=load-phases-error]"));
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_NoPhases_ShowsEmptyState()
    {
        // Arrange
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([]);

        // Act
        var cut = Render<Phases>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='phases-empty']"));
        Assert.Empty(cut.FindAll("table"));
    }

    [Fact]
    public void OnInitialized_ExistingPhases_RendersOneRowPerPhase()
    {
        // Arrange
        _apiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns([
                new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up"),
                new PhaseSummary(PhaseId.Parse("PHS-BBBBBB"), "Cool Down"),
            ]);

        // Act
        var cut = Render<Phases>();

        // Assert
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Warm Up", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Cool Down", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddPhase_ClickPlusPhase_AppendsRowInEditMode()
    {
        // Arrange
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([]);
        var created = new PhaseSummary(PhaseId.Parse("PHS-CCCCCC"), "New Phase");
        _apiClient
            .CreatePhaseAsync("New Phase", CancellationToken.None)
            .Returns(new CreatePhaseSucceeded(created));
        var cut = Render<Phases>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-phase-empty']").Click());

        // Assert
        await _apiClient.Received(1).CreatePhaseAsync("New Phase", CancellationToken.None);
        var input = cut.Find("[data-testid='name-input-PHS-CCCCCC']");
        Assert.True(input.HasAttribute("autofocus"));
    }

    [Fact]
    public async Task AddPhase_ServerRejectsDuplicateName_ShowsErrorAndAddsNoRow()
    {
        // Arrange
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([]);
        _apiClient
            .CreatePhaseAsync("New Phase", CancellationToken.None)
            .Returns(new CreatePhaseFailed("A phase named \"New Phase\" already exists."));
        var cut = Render<Phases>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-phase-empty']").Click());

        // Assert
        Assert.Contains(
            "A phase named \"New Phase\" already exists.",
            cut.Markup,
            StringComparison.Ordinal
        );
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public async Task RenamePhase_NameEditedAndSaveClicked_CallsRenameAndUpdatesDisplayedName()
    {
        // Arrange
        var phase = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up");
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([phase]);
        var renamed = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Cool Down");
        _apiClient
            .RenamePhaseAsync(PhaseId.Parse("PHS-AAAAAA"), "Cool Down", CancellationToken.None)
            .Returns(new RenamePhaseSucceeded(renamed));
        var cut = Render<Phases>();
        var input = cut.Find("[data-testid='name-input-PHS-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Cool Down"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-PHS-AAAAAA']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .RenamePhaseAsync(PhaseId.Parse("PHS-AAAAAA"), "Cool Down", CancellationToken.None);
        Assert.Contains("Cool Down", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-PHS-AAAAAA']"));
    }

    [Fact]
    public async Task RenamePhase_NameFailsLengthBound_ShowsErrorAndDoesNotCallApi()
    {
        // Arrange
        var phase = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up");
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([phase]);
        var cut = Render<Phases>();
        var input = cut.Find("[data-testid='name-input-PHS-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Ab"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-PHS-AAAAAA']").Click());

        // Assert
        await _apiClient
            .DidNotReceive()
            .RenamePhaseAsync(Arg.Any<PhaseId>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.NotEmpty(cut.FindAll("[data-testid='name-error-PHS-AAAAAA']"));
    }

    [Fact]
    public async Task RenamePhase_ServerRejects_ShowsErrorWithoutThrowing()
    {
        // Arrange
        var phase = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up");
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([phase]);
        _apiClient
            .RenamePhaseAsync(PhaseId.Parse("PHS-AAAAAA"), "Cool Down", CancellationToken.None)
            .Returns(new RenamePhaseFailed("name must be between 4 and 100 characters"));
        var cut = Render<Phases>();
        var input = cut.Find("[data-testid='name-input-PHS-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Cool Down"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-PHS-AAAAAA']").Click());

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
        var phase = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up");
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([phase]);

        // Act
        var cut = Render<Phases>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='save-PHS-AAAAAA']"));
    }

    [Fact]
    public async Task SaveButton_NameEdited_BecomesVisible()
    {
        // Arrange
        var phase = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up");
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([phase]);
        var cut = Render<Phases>();
        var input = cut.Find("[data-testid='name-input-PHS-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Cool Down"));

        // Assert
        Assert.Single(cut.FindAll("[data-testid='save-PHS-AAAAAA']"));
    }

    [Fact]
    public void RevertButton_NoUnsavedChanges_IsNotShown()
    {
        // Arrange
        var phase = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up");
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([phase]);

        // Act
        var cut = Render<Phases>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='revert-PHS-AAAAAA']"));
    }

    [Fact]
    public async Task RevertButton_NameEdited_BecomesVisible()
    {
        // Arrange
        var phase = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up");
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([phase]);
        var cut = Render<Phases>();
        var input = cut.Find("[data-testid='name-input-PHS-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Cool Down"));

        // Assert
        Assert.Single(cut.FindAll("[data-testid='revert-PHS-AAAAAA']"));
    }

    [Fact]
    public async Task RevertPhase_ClickRevert_RestoresOriginalNameAndHidesButtons()
    {
        // Arrange
        var phase = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up");
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([phase]);
        var cut = Render<Phases>();
        var input = cut.Find("[data-testid='name-input-PHS-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Cool Down"));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='revert-PHS-AAAAAA']").Click());

        // Assert
        Assert.Contains("value=\"Warm Up\"", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-PHS-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='revert-PHS-AAAAAA']"));
    }

    [Fact]
    public async Task RevertPhase_NameShowingValidationError_ClearsError()
    {
        // Arrange
        var phase = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up");
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([phase]);
        var cut = Render<Phases>();
        var input = cut.Find("[data-testid='name-input-PHS-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Ab"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-PHS-AAAAAA']").Click());
        Assert.NotEmpty(cut.FindAll("[data-testid='name-error-PHS-AAAAAA']"));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='revert-PHS-AAAAAA']").Click());

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='name-error-PHS-AAAAAA']"));
    }

    [Fact]
    public async Task DeletePhase_ClickDelete_CallsDeleteAndRemovesRow()
    {
        // Arrange
        var phase = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up");
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([phase]);
        _apiClient
            .DeletePhaseAsync(PhaseId.Parse("PHS-AAAAAA"), CancellationToken.None)
            .Returns(new DeletePhaseSucceeded());
        var cut = Render<Phases>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-PHS-AAAAAA']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .DeletePhaseAsync(PhaseId.Parse("PHS-AAAAAA"), CancellationToken.None);
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public async Task DeletePhase_ServerRejects_ShowsErrorAndKeepsRowWithoutThrowing()
    {
        // Arrange
        var phase = new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up");
        _apiClient.GetPhasesAsync(CancellationToken.None).Returns([phase]);
        _apiClient
            .DeletePhaseAsync(PhaseId.Parse("PHS-AAAAAA"), CancellationToken.None)
            .Returns(new DeletePhaseFailed("Request failed with status 500."));
        var cut = Render<Phases>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-PHS-AAAAAA']").Click());

        // Assert
        Assert.Contains("Request failed with status 500.", cut.Markup, StringComparison.Ordinal);
        Assert.Single(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_LoadFails_ShowsErrorWithoutThrowing()
    {
        // Arrange
        _apiClient
            .GetPhasesAsync(CancellationToken.None)
            .Returns<Task<IReadOnlyList<PhaseSummary>>>(_ =>
                throw new HttpRequestException("simulated failure")
            );

        // Act
        var cut = Render<Phases>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='load-phases-error']"));
        Assert.Empty(cut.FindAll("tbody tr"));
    }
}
