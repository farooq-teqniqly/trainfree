namespace Trainfree.Admin.Admin;

/// <summary>The upload succeeded; carries the updated exercise, including its new image URL.</summary>
internal sealed record UploadExerciseImageSucceeded(ExerciseSummary Exercise)
    : UploadExerciseImageOutcome;
