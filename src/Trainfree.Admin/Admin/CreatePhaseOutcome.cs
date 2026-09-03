namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to create a phase.</summary>
internal abstract record CreatePhaseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected CreatePhaseOutcome() { }
}

/// <summary>The create succeeded; carries the created phase.</summary>
internal sealed record CreatePhaseSucceeded(PhaseSummary Phase) : CreatePhaseOutcome;

/// <summary>The create was rejected; carries the server-supplied error message.</summary>
internal sealed record CreatePhaseFailed(string Error) : CreatePhaseOutcome;
