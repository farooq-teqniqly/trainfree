namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to create a session.</summary>
internal abstract record CreateSessionOutcome
{
    // Closes the hierarchy to the two outcomes declared in this folder.
    private protected CreateSessionOutcome() { }
}
