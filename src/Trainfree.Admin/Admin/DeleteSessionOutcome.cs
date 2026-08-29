namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to delete a session.</summary>
internal abstract record DeleteSessionOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected DeleteSessionOutcome() { }
}

/// <summary>
/// The delete succeeded, or the session was already gone (a 404 is treated as success --
/// the caller's desired end state, "this session no longer exists," already holds).
/// </summary>
internal sealed record DeleteSessionSucceeded : DeleteSessionOutcome;

/// <summary>The delete failed for a reason other than the session already being gone.</summary>
internal sealed record DeleteSessionFailed(string Error) : DeleteSessionOutcome;
