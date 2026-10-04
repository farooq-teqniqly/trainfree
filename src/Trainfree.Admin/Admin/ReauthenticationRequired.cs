namespace Trainfree.Admin.Admin;

/// <summary>
/// The response could not be parsed as the expected JSON shape, most often because the
/// Cloudflare Access session expired and Access answered with its own login page instead
/// of the Worker's response.
/// </summary>
internal sealed record ReauthenticationRequired : AccessCheckOutcome;
