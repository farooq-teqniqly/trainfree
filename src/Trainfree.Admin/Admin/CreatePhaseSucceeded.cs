namespace Trainfree.Admin.Admin;

/// <summary>The create succeeded; carries the created phase.</summary>
internal sealed record CreatePhaseSucceeded(PhaseSummary Phase) : CreatePhaseOutcome;
