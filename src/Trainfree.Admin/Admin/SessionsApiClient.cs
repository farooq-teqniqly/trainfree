using System.Net;
using System.Net.Http.Json;
using Trainfree.ApiClients;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="ISessionsApiClient"/>
internal sealed class SessionsApiClient : ApiClientBase, ISessionsApiClient
{
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
            return new CreateSessionFailed(
                await ReadErrorAsync(response, _logger, cancellationToken)
            );
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
            return new RenameSessionFailed(
                await ReadErrorAsync(response, _logger, cancellationToken)
            );
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

        return new DeleteSessionFailed(await ReadErrorAsync(response, _logger, cancellationToken));
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
}
