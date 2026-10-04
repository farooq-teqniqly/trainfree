namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to rename an exercise.</summary>
internal abstract record RenameExerciseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this folder.
    private protected RenameExerciseOutcome() { }
}
