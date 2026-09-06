namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to delete an exercise.</summary>
internal abstract record DeleteExerciseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected DeleteExerciseOutcome() { }
}

/// <summary>
/// The delete succeeded, or the exercise was already gone (a 404 is treated as success --
/// the caller's desired end state, "this exercise no longer exists," already holds).
/// </summary>
internal sealed record DeleteExerciseSucceeded : DeleteExerciseOutcome;

/// <summary>The delete failed for a reason other than the exercise already being gone.</summary>
internal sealed record DeleteExerciseFailed(string Error) : DeleteExerciseOutcome;
