using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Trainfree.Web.Versioning;

/// <inheritdoc cref="IVersionCheck"/>
internal sealed partial class VersionCheck : IVersionCheck
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly VersionStamp _running;
    private readonly ILogger<VersionCheck> _logger;

    public VersionCheck(HttpClient httpClient, VersionStamp running, ILogger<VersionCheck> logger)
    {
        _httpClient = httpClient;
        _running = running;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<VersionCheckOutcome> CheckAsync(CancellationToken cancellationToken = default)
    {
        // A local build has no deploy stamp to compare against -- the dev server and
        // `wrangler dev` disagree by construction, which would nag on every dev load.
        if (_running.IsLocalBuild)
        {
            return new VersionUnknown();
        }

        VersionDto? deployed;
        try
        {
            deployed = await _httpClient.GetFromJsonAsync<VersionDto>(
                "version",
                JsonOptions,
                cancellationToken
            );
        }
        catch (HttpRequestException ex)
        {
            LogVersionUnreachable(ex.Message);
            return new VersionUnknown();
        }
        catch (JsonException ex)
        {
            // Cloudflare Access answers an expired session with its own HTML login page,
            // which arrives here as unparseable JSON rather than a transport failure.
            LogVersionUnreadable(ex.Message);
            return new VersionUnknown();
        }

        if (deployed is null)
        {
            LogVersionUnreadable("The server returned an empty version document.");
            return new VersionUnknown();
        }

        var stamp = new VersionStamp(deployed.Version, deployed.Commit);
        return stamp == _running ? new RunningLatestVersion() : new RunningStaleVersion(stamp);
    }

    private sealed record VersionDto(string Version, string Commit);
}
