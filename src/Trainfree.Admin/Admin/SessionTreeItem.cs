namespace Trainfree.Admin.Admin;

/// <summary>A session with its session phases nested underneath.</summary>
internal sealed record SessionTreeItem(
    SessionSummary Session,
    IReadOnlyList<SessionPhaseTreeItem> Phases
);
