namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to delete a phase.</summary>
internal abstract record DeletePhaseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this folder.
    private protected DeletePhaseOutcome() { }
}
