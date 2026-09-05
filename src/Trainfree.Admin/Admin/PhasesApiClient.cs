using System.Net;
using System.Net.Http.Json;
using Trainfree.ApiClients;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="IPhasesApiClient"/>
internal sealed class PhasesApiClient : ApiClientBase, IPhasesApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PhasesApiClient> _logger;

    public PhasesApiClient(HttpClient httpClient, ILogger<PhasesApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PhaseSummary>> GetPhasesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var dtos = await _httpClient.GetFromJsonAsync<List<PhaseDto>>(
            "phases",
            JsonOptions,
            cancellationToken
        );

        return dtos?.ConvertAll(ToSummary) ?? [];
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public Task<CreatePhaseOutcome> CreatePhaseAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return ExecuteAsync<CreatePhaseOutcome>(
            async () =>
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "phases",
                    new { name },
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    return new CreatePhaseFailed(
                        await ReadErrorAsync(response, _logger, cancellationToken)
                    );
                }

                var dto = await response.Content.ReadFromJsonAsync<PhaseDto>(
                    JsonOptions,
                    cancellationToken
                );
                return new CreatePhaseSucceeded(ToSummary(dto!));
            },
            error => new CreatePhaseFailed(error),
            "Could not create phase. Try again.",
            _logger
        );
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public Task<RenamePhaseOutcome> RenamePhaseAsync(
        PhaseId id,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return ExecuteAsync<RenamePhaseOutcome>(
            async () =>
            {
                var response = await _httpClient.PatchAsJsonAsync(
                    $"phases/{id}",
                    new { name },
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    return new RenamePhaseFailed(
                        await ReadErrorAsync(response, _logger, cancellationToken)
                    );
                }

                var dto = await response.Content.ReadFromJsonAsync<PhaseDto>(
                    JsonOptions,
                    cancellationToken
                );
                return new RenamePhaseSucceeded(ToSummary(dto!));
            },
            error => new RenamePhaseFailed(error),
            "Could not rename phase. Try again.",
            _logger
        );
    }

    /// <inheritdoc/>
    public Task<DeletePhaseOutcome> DeletePhaseAsync(
        PhaseId id,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteAsync<DeletePhaseOutcome>(
            async () =>
            {
                var response = await _httpClient.DeleteAsync(
                    new Uri($"phases/{id}", UriKind.Relative),
                    cancellationToken
                );

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new DeletePhaseSucceeded();
                }

                return new DeletePhaseFailed(
                    await ReadErrorAsync(response, _logger, cancellationToken)
                );
            },
            error => new DeletePhaseFailed(error),
            "Could not delete phase. Try again.",
            _logger
        );

    private static PhaseSummary ToSummary(PhaseDto dto) => new(PhaseId.Parse(dto.Id), dto.Name);

    private sealed record PhaseDto(string Id, string Name, string CreatedAt, string UpdatedAt);
}
