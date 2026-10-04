namespace Trainfree.Admin.Admin;

/// <summary>The rename was rejected; carries the server-supplied error message.</summary>
internal sealed record RenamePhaseFailed(string Error) : RenamePhaseOutcome;
