namespace Trainfree.Admin.Admin;

/// <summary>The rename succeeded; carries the updated program.</summary>
internal sealed record RenameProgramSucceeded(ProgramSummary Program) : RenameProgramOutcome;
