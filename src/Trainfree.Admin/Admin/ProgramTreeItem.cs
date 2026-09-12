namespace Trainfree.Admin.Admin;

/// <summary>A program with its sessions nested underneath, as returned by the program tree endpoint.</summary>
internal sealed record ProgramTreeItem(
    ProgramSummary Program,
    IReadOnlyList<SessionTreeItem> Sessions
);
