namespace Trainfree.Admin.Admin;

/// <summary>The rename succeeded; carries the updated session.</summary>
internal sealed record RenameSessionSucceeded(SessionSummary Session) : RenameSessionOutcome;
