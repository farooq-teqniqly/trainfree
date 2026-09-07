namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to create a session phase.</summary>
internal abstract record CreateSessionPhaseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected CreateSessionPhaseOutcome() { }
}

/// <summary>The create succeeded; carries the created session phase.</summary>
internal sealed record CreateSessionPhaseSucceeded(SessionPhaseSummary SessionPhase)
    : CreateSessionPhaseOutcome;

/// <summary>The create was rejected; carries the server-supplied error message.</summary>
internal sealed record CreateSessionPhaseFailed(string Error) : CreateSessionPhaseOutcome;
