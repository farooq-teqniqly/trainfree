using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Pages;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Admin;

public sealed class ExercisesPageTests : BunitContext
{
    private readonly IExercisesApiClient _apiClient = Substitute.For<IExercisesApiClient>();

    public ExercisesPageTests() => Services.AddSingleton(_apiClient);

    [Fact]
    public void OnInitialized_ServerReturnsTheAccessLoginPage_ShowsTheLoadErrorInsteadOfFailing()
    {
        // Arrange
        _apiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns<IReadOnlyList<ExerciseSummary>>(_ =>
                throw new JsonException("'<' is an invalid start of a value.")
            );

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid=load-exercises-error]"));
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_NoExercises_ShowsEmptyState()
    {
        // Arrange
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([]);

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='exercises-empty']"));
        Assert.Empty(cut.FindAll("table"));
    }

    [Fact]
    public void OnInitialized_ExistingExercises_RendersOneRowPerExercise()
    {
        // Arrange
        _apiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns([
                new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat"),
                new ExerciseSummary(ExerciseId.Parse("EXR-BBBBBB"), "Skater Jump"),
            ]);

        // Act
        var cut = Render<Exercises>();

        // Assert
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Bodyweight Squat", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Skater Jump", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddExercise_ClickPlusExercise_AppendsRowInEditMode()
    {
        // Arrange
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([]);
        var created = new ExerciseSummary(ExerciseId.Parse("EXR-CCCCCC"), "New Exercise");
        _apiClient
            .CreateExerciseAsync("New Exercise", CancellationToken.None)
            .Returns(new CreateExerciseSucceeded(created));
        var cut = Render<Exercises>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-empty']").Click());

        // Assert
        await _apiClient.Received(1).CreateExerciseAsync("New Exercise", CancellationToken.None);
        var input = cut.Find("[data-testid='name-input-EXR-CCCCCC']");
        Assert.True(input.HasAttribute("autofocus"));
    }

    [Fact]
    public async Task AddExercise_ServerRejectsDuplicateName_ShowsErrorAndAddsNoRow()
    {
        // Arrange
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([]);
        _apiClient
            .CreateExerciseAsync("New Exercise", CancellationToken.None)
            .Returns(
                new CreateExerciseFailed("An exercise named \"New Exercise\" already exists.")
            );
        var cut = Render<Exercises>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-empty']").Click());

        // Assert
        Assert.Contains(
            "An exercise named \"New Exercise\" already exists.",
            cut.Markup,
            StringComparison.Ordinal
        );
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public async Task RenameExercise_NameEditedAndSaveClicked_CallsRenameAndUpdatesDisplayedName()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var renamed = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Skater Jump");
        _apiClient
            .RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                "Skater Jump",
                CancellationToken.None
            )
            .Returns(new RenameExerciseSucceeded(renamed));
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Skater Jump"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-EXR-AAAAAA']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                "Skater Jump",
                CancellationToken.None
            );
        Assert.Contains("Skater Jump", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task RenameExercise_NameFailsLengthBound_ShowsErrorAndDoesNotCallApi()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Ab"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-EXR-AAAAAA']").Click());

        // Assert
        await _apiClient
            .DidNotReceive()
            .RenameExerciseAsync(
                Arg.Any<ExerciseId>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
        Assert.NotEmpty(cut.FindAll("[data-testid='name-error-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task RenameExercise_ServerRejects_ShowsErrorWithoutThrowing()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        _apiClient
            .RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                "Skater Jump",
                CancellationToken.None
            )
            .Returns(new RenameExerciseFailed("name must be between 4 and 100 characters"));
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Skater Jump"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-EXR-AAAAAA']").Click());

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
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='save-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task SaveButton_NameEdited_BecomesVisible()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Skater Jump"));

        // Assert
        Assert.Single(cut.FindAll("[data-testid='save-EXR-AAAAAA']"));
    }

    [Fact]
    public void RevertButton_NoUnsavedChanges_IsNotShown()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='revert-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task RevertButton_NameEdited_BecomesVisible()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Skater Jump"));

        // Assert
        Assert.Single(cut.FindAll("[data-testid='revert-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task RevertExercise_ClickRevert_RestoresOriginalNameAndHidesButtons()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Skater Jump"));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='revert-EXR-AAAAAA']").Click());

        // Assert
        Assert.Contains("value=\"Bodyweight Squat\"", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-EXR-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='revert-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task RevertExercise_NameShowingValidationError_ClearsError()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Ab"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-EXR-AAAAAA']").Click());
        Assert.NotEmpty(cut.FindAll("[data-testid='name-error-EXR-AAAAAA']"));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='revert-EXR-AAAAAA']").Click());

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='name-error-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task DeleteExercise_ClickDelete_CallsDeleteAndRemovesRow()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        _apiClient
            .DeleteExerciseAsync(ExerciseId.Parse("EXR-AAAAAA"), CancellationToken.None)
            .Returns(new DeleteExerciseSucceeded());
        var cut = Render<Exercises>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-EXR-AAAAAA']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .DeleteExerciseAsync(ExerciseId.Parse("EXR-AAAAAA"), CancellationToken.None);
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public async Task DeleteExercise_ServerRejects_ShowsErrorAndKeepsRowWithoutThrowing()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        _apiClient
            .DeleteExerciseAsync(ExerciseId.Parse("EXR-AAAAAA"), CancellationToken.None)
            .Returns(new DeleteExerciseFailed("Request failed with status 500."));
        var cut = Render<Exercises>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-EXR-AAAAAA']").Click());

        // Assert
        Assert.Contains("Request failed with status 500.", cut.Markup, StringComparison.Ordinal);
        Assert.Single(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_LoadFails_ShowsErrorWithoutThrowing()
    {
        // Arrange
        _apiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns<Task<IReadOnlyList<ExerciseSummary>>>(_ =>
                throw new HttpRequestException("simulated failure")
            );

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='load-exercises-error']"));
        Assert.Empty(cut.FindAll("tbody tr"));
    }
}
