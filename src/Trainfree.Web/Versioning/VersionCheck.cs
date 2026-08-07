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
        // HttpClient reports its own timeout as a cancellation. The filter keeps a real
        // cancellation by the caller propagating -- only a timeout, where the supplied token
        // is still unsignalled, degrades to "unknown".
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogVersionUnreachable(ex.Message);
            return new VersionUnknown();
        }
        // Everything the deserialize path can throw for a response that is not the JSON we
        // expect. Cloudflare Access answers an expired session with its own HTML login page
        // (JsonException); a response whose charset the runtime cannot resolve surfaces as
        // InvalidOperationException, and an unsupported content type as NotSupportedException.
        // This component renders inside MainLayout, so an escaping exception here would take
        // down every page over a check that is only advisory.
        catch (Exception ex)
            when (ex is JsonException or InvalidOperationException or NotSupportedException)
        {
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
