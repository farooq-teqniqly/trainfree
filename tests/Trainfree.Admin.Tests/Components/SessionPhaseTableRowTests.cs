using Bunit;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Components;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Components;

public sealed class SessionPhaseTableRowTests : BunitContext
{
    private static SessionPhaseRow CreateRow() =>
        new(SessionPhaseId.Parse("SPH-AAAAAA"), PhaseId.Parse("PHS-AAAAAA"), "Warm Up");

    private IRenderedComponent<SessionPhaseTableRow> RenderRow(
        SessionPhaseRow row,
        bool isAddExerciseDisabled = false,
        bool isAddingExercise = false,
        IReadOnlyList<ExerciseSummary>? exerciseLibrary = null,
        string? exerciseLibraryLoadError = null,
        Action? onToggle = null,
        Action? onAddExercise = null,
        Action? onDelete = null,
        Action<AddProgramExerciseFormSubmission>? onSubmitExercise = null
    ) =>
        Render<SessionPhaseTableRow>(p =>
            p.Add(c => c.Phase, row)
                .Add(c => c.IsCollapsed, false)
                .Add(c => c.ChevronTitle, "Toggle")
                .Add(c => c.HasToggleableContent, true)
                .Add(c => c.IsAddExerciseDisabled, isAddExerciseDisabled)
                .Add(c => c.IsAddingExercise, isAddingExercise)
                .Add(c => c.ExerciseLibrary, exerciseLibrary ?? [])
                .Add(c => c.ExerciseLibraryLoadError, exerciseLibraryLoadError)
                .Add(c => c.OnToggle, onToggle ?? (() => { }))
                .Add(c => c.OnAddExercise, onAddExercise ?? (() => { }))
                .Add(c => c.OnDelete, onDelete ?? (() => { }))
                .Add(c => c.OnSubmitExercise, onSubmitExercise ?? (_ => { }))
        );

    [Fact]
    public void Render_Row_ShowsDisplayName()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = RenderRow(row);

        // Assert
        Assert.Equal(
            "Warm Up",
            cut.Find("[data-testid='phase-name-SPH-AAAAAA']").TextContent.Trim()
        );
    }

    [Fact]
    public void Click_AddExerciseButton_InvokesOnAddExercise()
    {
        // Arrange
        var row = CreateRow();
        var added = false;
        var cut = RenderRow(row, onAddExercise: () => added = true);

        // Act
        cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").Click();

        // Assert
        Assert.True(added);
    }

    [Fact]
    public void Render_IsAddExerciseDisabled_DisablesAddExerciseButton()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = RenderRow(row, isAddExerciseDisabled: true);

        // Assert
        Assert.True(cut.Find("[data-testid='add-exercise-SPH-AAAAAA']").HasAttribute("disabled"));
    }

    [Fact]
    public void Click_DeleteButton_InvokesOnDelete()
    {
        // Arrange
        var row = CreateRow();
        var deleted = false;
        var cut = RenderRow(row, onDelete: () => deleted = true);

        // Act
        cut.Find("[data-testid='phase-delete-SPH-AAAAAA']").Click();

        // Assert
        Assert.True(deleted);
    }

    [Fact]
    public void Click_ChevronButton_InvokesOnToggle()
    {
        // Arrange
        var row = CreateRow();
        var toggled = false;
        var cut = RenderRow(row, onToggle: () => toggled = true);

        // Act
        cut.Find("[data-testid='phase-chevron-SPH-AAAAAA']").Click();

        // Assert
        Assert.True(toggled);
    }

    [Fact]
    public void Render_IsAddingExerciseWithEmptyLibrary_ShowsEmptyLibraryMessage()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = RenderRow(row, isAddingExercise: true);

        // Assert
        Assert.Single(cut.FindAll("[data-testid='empty-exercise-library-SPH-AAAAAA']"));
    }

    [Fact]
    public void Render_IsAddingExerciseWithLibrary_ShowsAddExerciseForm()
    {
        // Arrange
        var row = CreateRow();
        var library = new[] { new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Push Up") };

        // Act
        var cut = RenderRow(row, isAddingExercise: true, exerciseLibrary: library);

        // Assert
        Assert.Single(cut.FindAll("[data-testid='exercise-picker-SPH-AAAAAA']"));
    }

    [Fact]
    public void Render_IsAddingExerciseWithLoadError_ShowsLoadError()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = RenderRow(
            row,
            isAddingExercise: true,
            exerciseLibraryLoadError: "Failed to load exercises."
        );

        // Assert
        Assert.Equal(
            "Failed to load exercises.",
            cut.Find("[data-testid='exercise-library-load-error-SPH-AAAAAA']").TextContent.Trim()
        );
    }

    [Fact]
    public void Render_RowHasError_ShowsInlineError()
    {
        // Arrange
        var row = CreateRow();
        row.Error = "Delete failed.";

        // Act
        var cut = RenderRow(row);

        // Assert
        Assert.Equal(
            "Delete failed.",
            cut.Find("[data-testid='phase-error-SPH-AAAAAA']").TextContent.Trim()
        );
    }
}
