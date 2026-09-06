namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to rename an exercise.</summary>
internal abstract record RenameExerciseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected RenameExerciseOutcome() { }
}

/// <summary>The rename succeeded; carries the updated exercise.</summary>
internal sealed record RenameExerciseSucceeded(ExerciseSummary Exercise) : RenameExerciseOutcome;

/// <summary>The rename was rejected; carries the server-supplied error message.</summary>
internal sealed record RenameExerciseFailed(string Error) : RenameExerciseOutcome;
