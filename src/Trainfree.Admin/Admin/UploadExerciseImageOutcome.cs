namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to upload or replace an exercise's image.</summary>
internal abstract record UploadExerciseImageOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected UploadExerciseImageOutcome() { }
}

/// <summary>The upload succeeded; carries the updated exercise, including its new image URL.</summary>
internal sealed record UploadExerciseImageSucceeded(ExerciseSummary Exercise)
    : UploadExerciseImageOutcome;

/// <summary>The upload was rejected; carries the server-supplied error message.</summary>
internal sealed record UploadExerciseImageFailed(string Error) : UploadExerciseImageOutcome;
