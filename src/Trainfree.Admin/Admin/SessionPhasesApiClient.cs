using System.Net.Http.Json;
using Trainfree.ApiClients;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="ISessionPhasesApiClient"/>
internal sealed class SessionPhasesApiClient : ApiClientBase, ISessionPhasesApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SessionPhasesApiClient> _logger;

    public SessionPhasesApiClient(HttpClient httpClient, ILogger<SessionPhasesApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SessionPhaseSummary>> GetSessionPhasesAsync(
        ProgramId programId,
        SessionId sessionId,
        CancellationToken cancellationToken = default
    )
    {
        var dtos = await _httpClient.GetFromJsonAsync<List<SessionPhaseDto>>(
            $"programs/{programId}/sessions/{sessionId}/phases",
            JsonOptions,
            cancellationToken
        );

        return dtos?.ConvertAll(ToSummary) ?? [];
    }

    /// <inheritdoc/>
    public Task<CreateSessionPhaseOutcome> CreateSessionPhaseAsync(
        ProgramId programId,
        SessionId sessionId,
        PhaseId phaseId,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteAsync<CreateSessionPhaseOutcome>(
            async () =>
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"programs/{programId}/sessions/{sessionId}/phases",
                    new { phaseId = phaseId.Value },
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    return new CreateSessionPhaseFailed(
                        await ReadErrorAsync(response, _logger, cancellationToken)
                    );
                }

                var dto = await response.Content.ReadFromJsonAsync<SessionPhaseDto>(
                    JsonOptions,
                    cancellationToken
                );
                return new CreateSessionPhaseSucceeded(ToSummary(dto!));
            },
            error => new CreateSessionPhaseFailed(error),
            "Could not add phase to session. Try again.",
            _logger
        );

    /// <inheritdoc/>
    public Task<DeleteSessionPhaseOutcome> DeleteSessionPhaseAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId id,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteAsync<DeleteSessionPhaseOutcome>(
            async () =>
            {
                var response = await _httpClient.DeleteAsync(
                    new Uri(
                        $"programs/{programId}/sessions/{sessionId}/phases/{id}",
                        UriKind.Relative
                    ),
                    cancellationToken
                );

                if (
                    response.IsSuccessStatusCode
                    || response.StatusCode == System.Net.HttpStatusCode.NotFound
                )
                {
                    return new DeleteSessionPhaseSucceeded();
                }

                return new DeleteSessionPhaseFailed(
                    await ReadErrorAsync(response, _logger, cancellationToken)
                );
            },
            error => new DeleteSessionPhaseFailed(error),
            "Could not delete session phase. Try again.",
            _logger
        );

    private static SessionPhaseSummary ToSummary(SessionPhaseDto dto) =>
        new(
            SessionPhaseId.Parse(dto.Id),
            SessionId.Parse(dto.SessionId),
            PhaseId.Parse(dto.PhaseId)
        );

    private sealed record SessionPhaseDto(
        string Id,
        string SessionId,
        string PhaseId,
        string CreatedAt
    );
}
