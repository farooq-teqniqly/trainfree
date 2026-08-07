namespace Trainfree.Web.Versioning;

/// <summary>Compares the build running in the browser against the build the server has deployed.</summary>
internal interface IVersionCheck
{
    /// <summary>Asks the server which build is deployed and compares it to the running one.</summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The comparison outcome; never throws for an unreachable or unauthenticated server.</returns>
    Task<VersionCheckOutcome> CheckAsync(CancellationToken cancellationToken = default);
}
