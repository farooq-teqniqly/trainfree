using System.Reflection;

namespace Trainfree.Web.Versioning;

/// <summary>Identifies one build of the app: its release version and the commit it came from.</summary>
/// <param name="Version">The release version, normally the git tag that triggered the deploy.</param>
/// <param name="Commit">The short commit SHA the build was produced from.</param>
internal sealed record VersionStamp(string Version, string Commit)
{
    private const string LocalBuild = "local";

    /// <summary>The stamp compiled into this assembly at publish time.</summary>
    /// <remarks>
    /// Read from the assembly rather than fetched at runtime on purpose: the point of the
    /// stamp is to identify the bundle the browser is actually running, so it has to travel
    /// inside that bundle.
    /// </remarks>
    public static VersionStamp Current { get; } =
        FromInformationalVersion(
            typeof(VersionStamp)
                .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
                ?? LocalBuild
        );

    /// <summary>The stamp as shown to the user.</summary>
    public string Display => $"{Version} ({Commit})";

    /// <summary>
    /// Whether this is an unstamped build, meaning a local <c>dotnet run</c> or
    /// <c>wrangler dev</c> rather than something deploy.yaml produced.
    /// </summary>
    public bool IsLocalBuild => Commit == LocalBuild;

    /// <summary>
    /// Parses the <c>&lt;version&gt;+&lt;commit&gt;</c> form that deploy.yaml passes to
    /// <c>dotnet publish</c> as <c>InformationalVersion</c>.
    /// </summary>
    /// <param name="informationalVersion">The assembly's informational version.</param>
    /// <returns>The parsed stamp; a build with no <c>+commit</c> suffix reports commit <c>local</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="informationalVersion"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="informationalVersion"/> is empty or whitespace.
    /// </exception>
    public static VersionStamp FromInformationalVersion(string informationalVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(informationalVersion);

        var separator = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        if (separator < 0)
        {
            return new VersionStamp(informationalVersion, LocalBuild);
        }

        var version = informationalVersion[..separator];

        // Belt and braces against the SDK's source-revision suffix ("+<commit>.<full-sha>"),
        // which Trainfree.Web.csproj disables: were it ever re-enabled, an unstripped suffix
        // would make every load look stale rather than fail visibly.
        var commit = informationalVersion[(separator + 1)..].Split('.')[0];
        return new VersionStamp(version, commit.Length > 0 ? commit : LocalBuild);
    }
}
