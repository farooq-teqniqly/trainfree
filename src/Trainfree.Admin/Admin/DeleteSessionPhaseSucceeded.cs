namespace Trainfree.Admin.Admin;

/// <summary>
/// The delete succeeded, or the session phase was already gone (a 404 is treated as
/// success -- the caller's desired end state, "this session phase no longer exists,"
/// already holds).
/// </summary>
internal sealed record DeleteSessionPhaseSucceeded : DeleteSessionPhaseOutcome;
