namespace Trainfree.Admin.Admin;

/// <summary>The create was rejected; carries the server-supplied error message.</summary>
internal sealed record CreateSessionPhaseFailed(string Error) : CreateSessionPhaseOutcome;
