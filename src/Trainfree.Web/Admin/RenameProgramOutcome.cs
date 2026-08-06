namespace Trainfree.Web.Admin;

/// <summary>The result of attempting to rename a program.</summary>
internal abstract record RenameProgramOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected RenameProgramOutcome() { }
}

/// <summary>The rename succeeded; carries the updated program.</summary>
internal sealed record RenameProgramSucceeded(ProgramSummary Program) : RenameProgramOutcome;

/// <summary>The rename was rejected; carries the server-supplied error message.</summary>
internal sealed record RenameProgramFailed(string Error) : RenameProgramOutcome;
