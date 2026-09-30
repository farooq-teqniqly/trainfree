namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to delete an exercise's image.</summary>
internal abstract record DeleteExerciseImageOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected DeleteExerciseImageOutcome() { }
}

/// <summary>
/// The image was deleted, or was already gone (a 404 is treated as success -- the caller's
/// desired end state, "this exercise has no image," already holds).
/// </summary>
internal sealed record DeleteExerciseImageSucceeded : DeleteExerciseImageOutcome;

/// <summary>
/// The delete failed for a reason other than the image already being gone; carries the
/// server-supplied error message.
/// </summary>
internal sealed record DeleteExerciseImageFailed(string Error) : DeleteExerciseImageOutcome;
