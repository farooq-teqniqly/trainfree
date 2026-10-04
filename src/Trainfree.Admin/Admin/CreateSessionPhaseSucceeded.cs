namespace Trainfree.Admin.Admin;

/// <summary>The create succeeded; carries the created session phase.</summary>
internal sealed record CreateSessionPhaseSucceeded(SessionPhaseSummary SessionPhase)
    : CreateSessionPhaseOutcome;
