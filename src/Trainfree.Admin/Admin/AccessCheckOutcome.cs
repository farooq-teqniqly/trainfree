namespace Trainfree.Admin.Admin;

/// <summary>The result of checking the caller's access to <c>Trainfree.Admin</c>.</summary>
internal abstract record AccessCheckOutcome
{
    // Closes the hierarchy to the four outcomes declared in this file.
    private protected AccessCheckOutcome() { }
}

/// <summary>The caller is a provisioned Administrator; the app renders normally.</summary>
/// <param name="User">The signed-in caller, for display in the shell.</param>
internal sealed record Administrator(CurrentUser User) : AccessCheckOutcome;

/// <summary>
/// The caller is unauthenticated, unprovisioned, or provisioned with a non-Administrator
/// role.
/// </summary>
internal sealed record NoAccess : AccessCheckOutcome;

/// <summary>
/// The check could not be completed -- a transport failure or a <c>5xx</c> response --
/// distinct from <see cref="NoAccess"/> so an outage is not misreported as a permission
/// denial.
/// </summary>
internal sealed record AccessCheckFailed : AccessCheckOutcome;

/// <summary>
/// The response could not be parsed as the expected JSON shape, most often because the
/// Cloudflare Access session expired and Access answered with its own login page instead
/// of the Worker's response.
/// </summary>
internal sealed record ReauthenticationRequired : AccessCheckOutcome;
