namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to create an exercise.</summary>
internal abstract record CreateExerciseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected CreateExerciseOutcome() { }
}

/// <summary>The create succeeded; carries the created exercise.</summary>
internal sealed record CreateExerciseSucceeded(ExerciseSummary Exercise) : CreateExerciseOutcome;

/// <summary>The create was rejected; carries the server-supplied error message.</summary>
internal sealed record CreateExerciseFailed(string Error) : CreateExerciseOutcome;
