namespace Trainfree.Admin.Admin;

/// <summary>The create succeeded; carries the created program.</summary>
internal sealed record CreateProgramSucceeded(ProgramSummary Program) : CreateProgramOutcome;
