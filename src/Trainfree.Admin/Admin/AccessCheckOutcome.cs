namespace Trainfree.Admin.Admin;

/// <summary>The result of checking the caller's access to <c>Trainfree.Admin</c>.</summary>
internal abstract record AccessCheckOutcome
{
    // Closes the hierarchy to the four outcomes declared in this folder.
    private protected AccessCheckOutcome() { }
}
