namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to rename a session.</summary>
internal abstract record RenameSessionOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected RenameSessionOutcome() { }
}

/// <summary>The rename succeeded; carries the updated session.</summary>
internal sealed record RenameSessionSucceeded(SessionSummary Session) : RenameSessionOutcome;

/// <summary>The rename was rejected; carries the server-supplied error message.</summary>
internal sealed record RenameSessionFailed(string Error) : RenameSessionOutcome;
