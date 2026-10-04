namespace Trainfree.Admin.Admin;

/// <summary>
/// The check could not be completed -- a transport failure or a <c>5xx</c> response --
/// distinct from <see cref="NoAccess"/> so an outage is not misreported as a permission
/// denial.
/// </summary>
internal sealed record AccessCheckFailed : AccessCheckOutcome;
