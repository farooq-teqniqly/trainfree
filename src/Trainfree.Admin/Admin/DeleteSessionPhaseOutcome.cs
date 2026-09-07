namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to delete a session phase.</summary>
internal abstract record DeleteSessionPhaseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected DeleteSessionPhaseOutcome() { }
}

/// <summary>
/// The delete succeeded, or the session phase was already gone (a 404 is treated as
/// success -- the caller's desired end state, "this session phase no longer exists,"
/// already holds).
/// </summary>
internal sealed record DeleteSessionPhaseSucceeded : DeleteSessionPhaseOutcome;

/// <summary>The delete failed for a reason other than the session phase already being gone.</summary>
internal sealed record DeleteSessionPhaseFailed(string Error) : DeleteSessionPhaseOutcome;
