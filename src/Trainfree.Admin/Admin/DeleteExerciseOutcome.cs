namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to delete an exercise.</summary>
internal abstract record DeleteExerciseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this folder.
    private protected DeleteExerciseOutcome() { }
}
