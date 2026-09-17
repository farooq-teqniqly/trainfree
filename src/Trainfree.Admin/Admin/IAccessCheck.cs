namespace Trainfree.Admin.Admin;

/// <summary>Checks the caller's access to <c>Trainfree.Admin</c> via <c>GET api/me</c>.</summary>
internal interface IAccessCheck
{
    /// <summary>Asks the server for the caller's identity and role.</summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The check's outcome; never throws for an unreachable or unauthenticated server.</returns>
    Task<AccessCheckOutcome> CheckAsync(CancellationToken cancellationToken = default);
}
