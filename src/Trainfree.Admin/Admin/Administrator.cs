namespace Trainfree.Admin.Admin;

/// <summary>The caller is a provisioned Administrator; the app renders normally.</summary>
/// <param name="User">The signed-in caller, for display in the shell.</param>
internal sealed record Administrator(CurrentUser User) : AccessCheckOutcome;
