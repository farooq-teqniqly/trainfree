namespace Trainfree.Versioning;

/// <summary>The result of comparing the running build against the deployed one.</summary>
public abstract record VersionCheckOutcome
{
    // Closes the hierarchy to the three outcomes declared in this file.
    private protected VersionCheckOutcome() { }
}

/// <summary>The browser is running the build that is currently deployed.</summary>
public sealed record RunningLatestVersion : VersionCheckOutcome;

/// <summary>The browser is running an older build than the one deployed.</summary>
/// <param name="Deployed">The stamp the server reported.</param>
public sealed record RunningStaleVersion(VersionStamp Deployed) : VersionCheckOutcome;

/// <summary>
/// The deployed version could not be determined, so staleness is unknown. Expected whenever
/// the app is offline or the Cloudflare Access session has expired, and deliberately not
/// surfaced to the user.
/// </summary>
public sealed record VersionUnknown : VersionCheckOutcome;
