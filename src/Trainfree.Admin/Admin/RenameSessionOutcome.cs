namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to rename a session.</summary>
internal abstract record RenameSessionOutcome
{
    // Closes the hierarchy to the two outcomes declared in this folder.
    private protected RenameSessionOutcome() { }
}
