namespace Trainfree.Web.Admin;

/// <summary>The result of attempting to delete a program.</summary>
internal abstract record DeleteProgramOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected DeleteProgramOutcome() { }
}

/// <summary>
/// The delete succeeded, or the program was already gone (a 404 is treated as success --
/// the caller's desired end state, "this program no longer exists," already holds).
/// </summary>
internal sealed record DeleteProgramSucceeded : DeleteProgramOutcome;

/// <summary>The delete failed for a reason other than the program already being gone.</summary>
internal sealed record DeleteProgramFailed(string Error) : DeleteProgramOutcome;
