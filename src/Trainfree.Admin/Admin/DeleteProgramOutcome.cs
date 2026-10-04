namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to delete a program.</summary>
internal abstract record DeleteProgramOutcome
{
    // Closes the hierarchy to the two outcomes declared in this folder.
    private protected DeleteProgramOutcome() { }
}
