using System.Net;
using System.Net.Http.Json;
using Trainfree.ApiClients;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="IProgramsApiClient"/>
internal sealed partial class ProgramsApiClient : ApiClientBase, IProgramsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProgramsApiClient> _logger;

    public ProgramsApiClient(HttpClient httpClient, ILogger<ProgramsApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProgramSummary>> GetProgramsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var dtos = await _httpClient.GetFromJsonAsync<List<ProgramDto>>(
            "programs",
            JsonOptions,
            cancellationToken
        );

        return dtos?.ConvertAll(ToSummary) ?? [];
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public async Task<CreateProgramOutcome> CreateProgramAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var response = await _httpClient.PostAsJsonAsync(
            "programs",
            new { name },
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return new CreateProgramFailed(
                await ReadErrorAsync(response, _logger, cancellationToken)
            );
        }

        var dto = await response.Content.ReadFromJsonAsync<ProgramDto>(
            JsonOptions,
            cancellationToken
        );
        return new CreateProgramSucceeded(ToSummary(dto!));
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public async Task<RenameProgramOutcome> RenameProgramAsync(
        ProgramId id,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var response = await _httpClient.PatchAsJsonAsync(
            $"programs/{id}",
            new { name },
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return new RenameProgramFailed(
                await ReadErrorAsync(response, _logger, cancellationToken)
            );
        }

        var dto = await response.Content.ReadFromJsonAsync<ProgramDto>(
            JsonOptions,
            cancellationToken
        );
        return new RenameProgramSucceeded(ToSummary(dto!));
    }

    /// <inheritdoc/>
    public async Task<DeleteProgramOutcome> DeleteProgramAsync(
        ProgramId id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.DeleteAsync(
            new Uri($"programs/{id}", UriKind.Relative),
            cancellationToken
        );

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
        {
            return new DeleteProgramSucceeded();
        }

        return new DeleteProgramFailed(await ReadErrorAsync(response, _logger, cancellationToken));
    }

    private static ProgramSummary ToSummary(ProgramDto dto) =>
        new(ProgramId.Parse(dto.Id), dto.Name);

    private sealed record ProgramDto(string Id, string Name, string CreatedAt, string UpdatedAt);
}
