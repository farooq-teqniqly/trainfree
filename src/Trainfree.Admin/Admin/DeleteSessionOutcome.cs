namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to delete a session.</summary>
internal abstract record DeleteSessionOutcome
{
    // Closes the hierarchy to the two outcomes declared in this folder.
    private protected DeleteSessionOutcome() { }
}
