using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Trainfree.ApiClients;

/// <summary>
/// Base class for Blazor <c>*ApiClient</c> implementations, providing JSON options and
/// Worker error-response reading shared across every client.
/// </summary>
public abstract partial class ApiClientBase
{
    /// <summary>
    /// JSON serializer options shared by every API client reading Worker responses.
    /// </summary>
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Reads a human-readable error message from a failed Worker <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="response">The failed response to read.</param>
    /// <param name="logger">
    /// The calling client's own logger, so an unreadable body is logged under that
    /// client's category rather than this shared implementation's.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>
    /// The parsed error message, or a generic <c>"Request failed with status {code}."</c>
    /// fallback when the body is not JSON or cannot be parsed.
    /// </returns>
    protected static async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        var fallback = $"Request failed with status {(int)response.StatusCode}.";

        // Only the Worker's own errors are JSON. A failure that never reached it -- most
        // often Cloudflare Access answering an expired session with a 302 and an HTML login
        // page -- would otherwise throw out of here and take down the page, since the
        // callers handle outcomes rather than exceptions.
        if (response.Content.Headers.ContentType?.MediaType is not "application/json")
        {
            return fallback;
        }

        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorDto>(
                JsonOptions,
                cancellationToken
            );
            return body?.Error ?? fallback;
        }
        // A body labeled JSON that is not (an intermediary's error page with the wrong
        // content type, a truncated response). Callers handle outcomes, not exceptions, so
        // failing to read the reason must not become a failure to report one.
        catch (Exception ex)
            when (ex is JsonException or InvalidOperationException or NotSupportedException)
        {
            LogErrorBodyUnreadable(
                logger,
                response.RequestMessage?.RequestUri?.ToString(),
                response.Content.Headers.ContentType?.MediaType,
                (int)response.StatusCode,
                ex
            );
            return fallback;
        }
    }

    private sealed record ErrorDto(string Error);
}
