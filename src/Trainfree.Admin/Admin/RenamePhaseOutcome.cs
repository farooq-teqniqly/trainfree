namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to rename a phase.</summary>
internal abstract record RenamePhaseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected RenamePhaseOutcome() { }
}

/// <summary>The rename succeeded; carries the updated phase.</summary>
internal sealed record RenamePhaseSucceeded(PhaseSummary Phase) : RenamePhaseOutcome;

/// <summary>The rename was rejected; carries the server-supplied error message.</summary>
internal sealed record RenamePhaseFailed(string Error) : RenamePhaseOutcome;
