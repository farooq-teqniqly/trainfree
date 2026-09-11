using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Trainfree.Admin.Components;
using Trainfree.Admin.Pages;
using Trainfree.Domain.Ids;
using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Tests.Components;

public sealed class ProgramExerciseTableRowTests : BunitContext
{
    private static Programs.ProgramExerciseRow CreateRow() =>
        new(
            new RepsProgramExercise(
                ProgramExerciseId.Parse("PGX-AAAAAA"),
                SessionPhaseId.Parse("SPH-AAAAAA"),
                ExerciseId.Parse("EXR-AAAAAA"),
                10,
                45,
                new SetPrescription(3, 60),
                ProgramExerciseSide.Both
            ),
            "Bodyweight Squat"
        );

    [Fact]
    public void Render_Row_ShowsExerciseValues()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = Render<ProgramExerciseTableRow>(p =>
            p.Add(c => c.Row, row).Add(c => c.OnSave, () => { })
        );

        // Assert
        Assert.Equal(
            "Bodyweight Squat",
            cut.Find("[data-testid='program-exercise-name-PGX-AAAAAA']").TextContent.Trim()
        );
        Assert.Equal(
            "10",
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").GetAttribute("value")
        );
        Assert.Equal(
            "45",
            cut.Find("[data-testid='program-exercise-weight-PGX-AAAAAA']").GetAttribute("value")
        );
        Assert.Equal(
            "3",
            cut.Find("[data-testid='program-exercise-sets-PGX-AAAAAA']").GetAttribute("value")
        );
        Assert.Equal(
            "60",
            cut.Find("[data-testid='program-exercise-rest-PGX-AAAAAA']").GetAttribute("value")
        );
    }

    [Fact]
    public void Render_RowIsClean_HidesSaveAndRevertButtons()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = Render<ProgramExerciseTableRow>(p =>
            p.Add(c => c.Row, row).Add(c => c.OnSave, () => { })
        );

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='program-exercise-save-PGX-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='program-exercise-revert-PGX-AAAAAA']"));
    }

    [Fact]
    public void Input_CountFieldEdited_ShowsSaveAndRevertButtons()
    {
        // Arrange
        var row = CreateRow();
        var cut = Render<ProgramExerciseTableRow>(p =>
            p.Add(c => c.Row, row).Add(c => c.OnSave, () => { })
        );

        // Act
        cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").Input("12");

        // Assert
        Assert.Single(cut.FindAll("[data-testid='program-exercise-save-PGX-AAAAAA']"));
        Assert.Single(cut.FindAll("[data-testid='program-exercise-revert-PGX-AAAAAA']"));
    }

    [Fact]
    public void Click_SaveButton_InvokesOnSave()
    {
        // Arrange
        var row = CreateRow();
        var saved = false;
        var cut = Render<ProgramExerciseTableRow>(p =>
            p.Add(c => c.Row, row).Add(c => c.OnSave, () => saved = true)
        );
        cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").Input("12");

        // Act
        cut.Find("[data-testid='program-exercise-save-PGX-AAAAAA']").Click();

        // Assert
        Assert.True(saved);
    }

    [Fact]
    public void Click_RevertButton_RestoresSavedValuesWithoutInvokingOnSave()
    {
        // Arrange
        var row = CreateRow();
        var saved = false;
        var cut = Render<ProgramExerciseTableRow>(p =>
            p.Add(c => c.Row, row).Add(c => c.OnSave, () => saved = true)
        );
        cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").Input("99");

        // Act
        cut.Find("[data-testid='program-exercise-revert-PGX-AAAAAA']").Click();

        // Assert
        Assert.Equal(
            "10",
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").GetAttribute("value")
        );
        Assert.False(saved);
        Assert.Empty(cut.FindAll("[data-testid='program-exercise-save-PGX-AAAAAA']"));
    }

    [Fact]
    public void KeyDown_EnterOnDirtyField_InvokesOnSave()
    {
        // Arrange
        var row = CreateRow();
        var saved = false;
        var cut = Render<ProgramExerciseTableRow>(p =>
            p.Add(c => c.Row, row).Add(c => c.OnSave, () => saved = true)
        );
        var input = cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']");
        input.Input("12");

        // Act
        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        Assert.True(saved);
    }

    [Fact]
    public void KeyDown_EnterOnCleanField_DoesNotInvokeOnSave()
    {
        // Arrange
        var row = CreateRow();
        var saved = false;
        var cut = Render<ProgramExerciseTableRow>(p =>
            p.Add(c => c.Row, row).Add(c => c.OnSave, () => saved = true)
        );

        // Act
        cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']")
            .KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        Assert.False(saved);
    }

    [Fact]
    public void Click_DuplicateButton_InvokesOnDuplicate()
    {
        // Arrange
        var row = CreateRow();
        var duplicated = false;
        var cut = Render<ProgramExerciseTableRow>(p =>
            p.Add(c => c.Row, row)
                .Add(c => c.OnSave, () => { })
                .Add(c => c.OnDuplicate, () => duplicated = true)
        );

        // Act
        cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']").Click();

        // Assert
        Assert.True(duplicated);
    }

    [Fact]
    public void Click_DeleteButton_InvokesOnDelete()
    {
        // Arrange
        var row = CreateRow();
        var deleted = false;
        var cut = Render<ProgramExerciseTableRow>(p =>
            p.Add(c => c.Row, row)
                .Add(c => c.OnSave, () => { })
                .Add(c => c.OnDelete, () => deleted = true)
        );

        // Act
        cut.Find("[data-testid='program-exercise-delete-PGX-AAAAAA']").Click();

        // Assert
        Assert.True(deleted);
    }

    [Fact]
    public void Render_RowIsSaving_DisablesFieldsAndActionButtons()
    {
        // Arrange
        var row = CreateRow();
        row.IsSaving = true;

        // Act
        var cut = Render<ProgramExerciseTableRow>(p =>
            p.Add(c => c.Row, row).Add(c => c.OnSave, () => { })
        );

        // Assert
        Assert.True(
            cut.Find("[data-testid='program-exercise-count-PGX-AAAAAA']").HasAttribute("disabled")
        );
        Assert.True(
            cut.Find("[data-testid='program-exercise-duplicate-PGX-AAAAAA']")
                .HasAttribute("disabled")
        );
        Assert.True(
            cut.Find("[data-testid='program-exercise-delete-PGX-AAAAAA']").HasAttribute("disabled")
        );
    }
}
