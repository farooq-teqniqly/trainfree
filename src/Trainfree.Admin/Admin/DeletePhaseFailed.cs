namespace Trainfree.Admin.Admin;

/// <summary>The delete failed for a reason other than the phase already being gone.</summary>
internal sealed record DeletePhaseFailed(string Error) : DeletePhaseOutcome;
