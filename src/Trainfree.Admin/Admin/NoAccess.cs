namespace Trainfree.Admin.Admin;

/// <summary>
/// The caller is unauthenticated, unprovisioned, or provisioned with a non-Administrator
/// role.
/// </summary>
internal sealed record NoAccess : AccessCheckOutcome;
