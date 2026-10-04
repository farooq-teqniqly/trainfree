namespace Trainfree.Admin.Admin;

/// <summary>The delete failed for a reason other than the session phase already being gone.</summary>
internal sealed record DeleteSessionPhaseFailed(string Error) : DeleteSessionPhaseOutcome;
