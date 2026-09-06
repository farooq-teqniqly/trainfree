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
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="response"/> or <paramref name="logger"/> is
    /// <see langword="null"/>.
    /// </exception>
    protected static async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(logger);

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

    /// <summary>
    /// Runs a mutation operation, catching transport and parse exceptions and converting
    /// them into the operation's own <c>Failed</c> outcome instead of letting them
    /// propagate out of the calling Blazor event handler.
    /// </summary>
    /// <typeparam name="TOutcome">The operation's outcome type.</typeparam>
    /// <param name="operation">The request/outcome-mapping body to run.</param>
    /// <param name="onFailure">Builds the operation's <c>Failed</c> outcome from a message.</param>
    /// <param name="failureMessage">The caller-facing message passed to <paramref name="onFailure"/> on a guarded exception.</param>
    /// <param name="logger">The calling client's own logger, so the guarded exception is logged under that client's category.</param>
    /// <returns>
    /// The result of <paramref name="operation"/>, or the outcome produced by
    /// <paramref name="onFailure"/> when a guarded exception is caught.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="operation"/>, <paramref name="onFailure"/>, or
    /// <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="failureMessage"/> is empty or whitespace.
    /// </exception>
    protected static async Task<TOutcome> ExecuteAsync<TOutcome>(
        Func<Task<TOutcome>> operation,
        Func<string, TOutcome> onFailure,
        string failureMessage,
        ILogger logger
    )
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(onFailure);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            return await operation();
        }
        // Mirrors the exception set OnInitializedAsync already catches around the initial
        // load: a transport failure, a redirected Cloudflare Access login page arriving as
        // malformed JSON, or a canceled request must degrade to the Failed outcome instead
        // of propagating out of the calling event handler. FormatException covers a
        // successful response whose id doesn't match its domain type's expected shape.
        catch (Exception ex)
            when (ex
                    is HttpRequestException
                        or JsonException
                        or InvalidOperationException
                        or NotSupportedException
                        or OperationCanceledException
                        or FormatException
            )
        {
            LogMutationExceptionCaught(logger, ex);
            return onFailure(failureMessage);
        }
    }

    private sealed record ErrorDto(string Error);
}
