using System.Net;
using System.Net.Http.Json;
using Trainfree.ApiClients;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="IPhasesApiClient"/>
internal sealed partial class PhasesApiClient : ApiClientBase, IPhasesApiClient
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
    public async Task<CreatePhaseOutcome> CreatePhaseAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var response = await _httpClient.PostAsJsonAsync("phases", new { name }, cancellationToken);

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
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public async Task<RenamePhaseOutcome> RenamePhaseAsync(
        PhaseId id,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

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
    }

    /// <inheritdoc/>
    public async Task<DeletePhaseOutcome> DeletePhaseAsync(
        PhaseId id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.DeleteAsync(
            new Uri($"phases/{id}", UriKind.Relative),
            cancellationToken
        );

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
        {
            return new DeletePhaseSucceeded();
        }

        return new DeletePhaseFailed(await ReadErrorAsync(response, _logger, cancellationToken));
    }

    private static PhaseSummary ToSummary(PhaseDto dto) => new(PhaseId.Parse(dto.Id), dto.Name);

    private sealed record PhaseDto(string Id, string Name, string CreatedAt, string UpdatedAt);
}
