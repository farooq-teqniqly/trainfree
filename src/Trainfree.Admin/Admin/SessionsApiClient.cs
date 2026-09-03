using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="ISessionsApiClient"/>
internal sealed partial class SessionsApiClient : ISessionsApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<SessionsApiClient> _logger;

    public SessionsApiClient(HttpClient httpClient, ILogger<SessionsApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(
        ProgramId programId,
        CancellationToken cancellationToken = default
    )
    {
        var dtos = await _httpClient.GetFromJsonAsync<List<SessionDto>>(
            $"programs/{programId}/sessions",
            JsonOptions,
            cancellationToken
        );

        return dtos?.ConvertAll(ToSummary) ?? [];
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public async Task<CreateSessionOutcome> CreateSessionAsync(
        ProgramId programId,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var response = await _httpClient.PostAsJsonAsync(
            $"programs/{programId}/sessions",
            new { name },
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return new CreateSessionFailed(await ReadErrorAsync(response, cancellationToken));
        }

        var dto = await response.Content.ReadFromJsonAsync<SessionDto>(
            JsonOptions,
            cancellationToken
        );
        return new CreateSessionSucceeded(ToSummary(dto!));
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public async Task<RenameSessionOutcome> RenameSessionAsync(
        ProgramId programId,
        SessionId id,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var response = await _httpClient.PatchAsJsonAsync(
            $"programs/{programId}/sessions/{id}",
            new { name },
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return new RenameSessionFailed(await ReadErrorAsync(response, cancellationToken));
        }

        var dto = await response.Content.ReadFromJsonAsync<SessionDto>(
            JsonOptions,
            cancellationToken
        );
        return new RenameSessionSucceeded(ToSummary(dto!));
    }

    /// <inheritdoc/>
    public async Task<DeleteSessionOutcome> DeleteSessionAsync(
        ProgramId programId,
        SessionId id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.DeleteAsync(
            new Uri($"programs/{programId}/sessions/{id}", UriKind.Relative),
            cancellationToken
        );

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
        {
            return new DeleteSessionSucceeded();
        }

        return new DeleteSessionFailed(await ReadErrorAsync(response, cancellationToken));
    }

    private async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
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
                _logger,
                response.RequestMessage?.RequestUri?.ToString(),
                response.Content.Headers.ContentType?.MediaType,
                (int)response.StatusCode,
                ex
            );
            return fallback;
        }
    }

    private static SessionSummary ToSummary(SessionDto dto) =>
        new(SessionId.Parse(dto.Id), ProgramId.Parse(dto.ProgramId), dto.Name);

    private sealed record SessionDto(
        string Id,
        string ProgramId,
        string Name,
        string CreatedAt,
        string UpdatedAt
    );

    private sealed record ErrorDto(string Error);
}
