using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="IAccessCheck"/>
internal sealed partial class AccessCheck : IAccessCheck
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<AccessCheck> _logger;

    public AccessCheck(HttpClient httpClient, ILogger<AccessCheck> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AccessCheckOutcome> CheckAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(
                new Uri("me", UriKind.Relative),
                cancellationToken
            );
        }
        catch (HttpRequestException ex)
        {
            LogAccessCheckUnreachable(ex.Message);
            return new AccessCheckFailed();
        }
        // HttpClient reports its own timeout as a cancellation. The filter keeps a real
        // cancellation by the caller propagating -- only a timeout, where the supplied token
        // is still unsignalled, degrades to AccessCheckFailed.
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogAccessCheckUnreachable(ex.Message);
            return new AccessCheckFailed();
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return new NoAccess();
        }

        if ((int)response.StatusCode >= 500)
        {
            LogAccessCheckServerError((int)response.StatusCode);
            return new AccessCheckFailed();
        }

        MeDto? me;
        try
        {
            me = await response.Content.ReadFromJsonAsync<MeDto>(JsonOptions, cancellationToken);
        }
        // A Cloudflare Access expired session answers with its own HTML login page instead
        // of the Worker's JSON, same underlying cause VersionCheck.cs already documents for
        // GET /api/version.
        catch (Exception ex)
            when (ex is JsonException or InvalidOperationException or NotSupportedException)
        {
            LogAccessCheckUnreadable(ex.Message);
            return new ReauthenticationRequired();
        }

        if (me is null)
        {
            LogAccessCheckUnreadable("The server returned an empty identity document.");
            return new ReauthenticationRequired();
        }

        return string.Equals(me.Role, "Administrator", StringComparison.Ordinal)
            ? new Administrator()
            : new NoAccess();
    }

    private sealed record MeDto(string Email, string Role);
}
