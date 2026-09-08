namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to delete a program exercise.</summary>
internal abstract record DeleteProgramExerciseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected DeleteProgramExerciseOutcome() { }
}

/// <summary>
/// The delete succeeded, or the program exercise was already gone (a 404 is treated as
/// success -- the caller's desired end state, "this program exercise no longer exists,"
/// already holds).
/// </summary>
internal sealed record DeleteProgramExerciseSucceeded : DeleteProgramExerciseOutcome;

/// <summary>The delete failed for a reason other than the program exercise already being gone.</summary>
internal sealed record DeleteProgramExerciseFailed(string Error) : DeleteProgramExerciseOutcome;
