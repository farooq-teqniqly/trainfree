namespace Trainfree.Admin.Admin;

/// <summary>The rename was rejected; carries the server-supplied error message.</summary>
internal sealed record RenameProgramFailed(string Error) : RenameProgramOutcome;
