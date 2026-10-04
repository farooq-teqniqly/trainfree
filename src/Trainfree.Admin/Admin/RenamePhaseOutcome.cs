namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to rename a phase.</summary>
internal abstract record RenamePhaseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this folder.
    private protected RenamePhaseOutcome() { }
}
