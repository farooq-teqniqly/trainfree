namespace Trainfree.Admin.Admin;

/// <summary>The create succeeded; carries the created session.</summary>
internal sealed record CreateSessionSucceeded(SessionSummary Session) : CreateSessionOutcome;
