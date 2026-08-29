namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to create a session.</summary>
internal abstract record CreateSessionOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected CreateSessionOutcome() { }
}

/// <summary>The create succeeded; carries the created session.</summary>
internal sealed record CreateSessionSucceeded(SessionSummary Session) : CreateSessionOutcome;

/// <summary>The create was rejected; carries the server-supplied error message.</summary>
internal sealed record CreateSessionFailed(string Error) : CreateSessionOutcome;
