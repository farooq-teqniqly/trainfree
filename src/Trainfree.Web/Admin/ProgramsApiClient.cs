using System.Net.Http.Json;
using System.Text.Json;
using Trainfree.Web.Ids;

namespace Trainfree.Web.Admin;

/// <inheritdoc cref="IProgramsApiClient"/>
internal sealed class ProgramsApiClient : IProgramsApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    public ProgramsApiClient(HttpClient httpClient) => _httpClient = httpClient;

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
    public async Task<ProgramSummary> CreateProgramAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PostAsJsonAsync(
            "programs",
            new { name },
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ProgramDto>(
            JsonOptions,
            cancellationToken
        );
        return ToSummary(dto!);
    }

    /// <inheritdoc/>
    public async Task<ProgramSummary> RenameProgramAsync(
        ProgramId id,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PatchAsJsonAsync(
            $"programs/{id}",
            new { name },
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ProgramDto>(
            JsonOptions,
            cancellationToken
        );
        return ToSummary(dto!);
    }

    /// <inheritdoc/>
    public async Task DeleteProgramAsync(
        ProgramId id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.DeleteAsync(
            new Uri($"programs/{id}", UriKind.Relative),
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }

    private static ProgramSummary ToSummary(ProgramDto dto) =>
        new(ProgramId.Parse(dto.Id), dto.Name);

    private sealed record ProgramDto(string Id, string Name, string CreatedAt, string UpdatedAt);
}
