using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Components;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Components;

public sealed class AddExerciseFormTests : BunitContext
{
    private static readonly IReadOnlyList<ExerciseSummary> Exercises =
    [
        new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat"),
        new ExerciseSummary(ExerciseId.Parse("EXR-BBBBBB"), "Push Up"),
    ];

    private IRenderedComponent<AddExerciseForm> RenderForm(
        Action<AddProgramExerciseFormSubmission>? onSubmit = null
    ) =>
        Render<AddExerciseForm>(p =>
            p.Add(c => c.PhaseId, SessionPhaseId.Parse("SPH-AAAAAA"))
                .Add(c => c.Exercises, Exercises)
                .Add(c => c.OnSubmit, onSubmit ?? (_ => { }))
        );

    private static void FillValidForm(IRenderedComponent<AddExerciseForm> cut)
    {
        cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']").Change("EXR-AAAAAA");
        cut.Find("[data-testid='exercise-count-SPH-AAAAAA']").Input("10");
        cut.Find("[data-testid='exercise-sets-SPH-AAAAAA']").Input("3");
        cut.Find("[data-testid='exercise-restseconds-SPH-AAAAAA']").Input("60");
    }

    [Fact]
    public void Render_Exercises_ListsThemInThePicker()
    {
        // Arrange / Act
        var cut = RenderForm();

        // Assert
        Assert.Contains("Bodyweight Squat", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Push Up", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_NoFieldsFilled_DisablesSubmitButton()
    {
        // Arrange / Act
        var cut = RenderForm();

        // Assert
        Assert.True(
            cut.Find("[data-testid='add-exercise-submit-SPH-AAAAAA']").HasAttribute("disabled")
        );
    }

    [Fact]
    public void Click_Submit_AllFieldsFilled_InvokesOnSubmitWithEnteredValues()
    {
        // Arrange
        AddProgramExerciseFormSubmission? submission = null;
        var cut = RenderForm(onSubmit: s => submission = s);
        FillValidForm(cut);

        // Act
        cut.Find("[data-testid='add-exercise-submit-SPH-AAAAAA']").Click();

        // Assert
        Assert.NotNull(submission);
        Assert.Equal(ExerciseId.Parse("EXR-AAAAAA"), submission.ExerciseId);
        Assert.False(submission.IsTimed);
        Assert.Equal(10, submission.Count);
        Assert.Equal(3, submission.Sets);
        Assert.Equal(60, submission.RestSeconds);
    }

    [Fact]
    public void Click_Submit_TypeChangedToTimed_InvokesOnSubmitWithIsTimedTrue()
    {
        // Arrange
        AddProgramExerciseFormSubmission? submission = null;
        var cut = RenderForm(onSubmit: s => submission = s);
        cut.Find("[data-testid='exercise-type-SPH-AAAAAA']").Change("Timed");
        FillValidForm(cut);

        // Act
        cut.Find("[data-testid='add-exercise-submit-SPH-AAAAAA']").Click();

        // Assert
        Assert.NotNull(submission);
        Assert.True(submission.IsTimed);
    }

    [Fact]
    public void KeyDown_EnterWithAllFieldsFilled_InvokesOnSubmit()
    {
        // Arrange
        var submitted = false;
        var cut = RenderForm(onSubmit: _ => submitted = true);
        FillValidForm(cut);

        // Act
        cut.Find("[data-testid='exercise-count-SPH-AAAAAA']")
            .KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        Assert.True(submitted);
    }

    [Fact]
    public void KeyDown_EnterWithMissingFields_DoesNotInvokeOnSubmit()
    {
        // Arrange
        var submitted = false;
        var cut = RenderForm(onSubmit: _ => submitted = true);
        cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']").Change("EXR-AAAAAA");

        // Act
        cut.Find("[data-testid='exercise-count-SPH-AAAAAA']")
            .KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        Assert.False(submitted);
    }

    [Fact]
    public void Render_IsSubmittingIsTrue_DisablesEveryField()
    {
        // Arrange
        var cut = Render<AddExerciseForm>(p =>
            p.Add(c => c.PhaseId, SessionPhaseId.Parse("SPH-AAAAAA"))
                .Add(c => c.Exercises, Exercises)
                .Add(c => c.IsSubmitting, false)
                .Add(c => c.OnSubmit, _ => { })
        );
        FillValidForm(cut);

        // Act
        cut.Render(p => p.Add(c => c.IsSubmitting, true));

        // Assert
        Assert.True(
            cut.Find("[data-testid='exercise-picker-SPH-AAAAAA']").HasAttribute("disabled")
        );
        Assert.True(cut.Find("[data-testid='exercise-type-SPH-AAAAAA']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='exercise-count-SPH-AAAAAA']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='exercise-sets-SPH-AAAAAA']").HasAttribute("disabled"));
        Assert.True(
            cut.Find("[data-testid='exercise-restseconds-SPH-AAAAAA']").HasAttribute("disabled")
        );
        Assert.True(
            cut.Find("[data-testid='add-exercise-submit-SPH-AAAAAA']").HasAttribute("disabled")
        );
    }
}
