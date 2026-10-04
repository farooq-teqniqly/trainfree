namespace Trainfree.Admin.Admin;

/// <summary>The rename was rejected; carries the server-supplied error message.</summary>
internal sealed record RenameSessionFailed(string Error) : RenameSessionOutcome;
