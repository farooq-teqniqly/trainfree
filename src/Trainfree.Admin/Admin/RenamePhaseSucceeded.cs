namespace Trainfree.Admin.Admin;

/// <summary>The rename succeeded; carries the updated phase.</summary>
internal sealed record RenamePhaseSucceeded(PhaseSummary Phase) : RenamePhaseOutcome;
