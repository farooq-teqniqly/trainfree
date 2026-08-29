namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to create a program.</summary>
internal abstract record CreateProgramOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected CreateProgramOutcome() { }
}

/// <summary>The create succeeded; carries the created program.</summary>
internal sealed record CreateProgramSucceeded(ProgramSummary Program) : CreateProgramOutcome;

/// <summary>The create was rejected; carries the server-supplied error message.</summary>
internal sealed record CreateProgramFailed(string Error) : CreateProgramOutcome;
