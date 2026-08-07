using System.Net;
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
    public async Task<CreateProgramOutcome> CreateProgramAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PostAsJsonAsync(
            "programs",
            new { name },
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return new CreateProgramFailed(await ReadErrorAsync(response, cancellationToken));
        }

        var dto = await response.Content.ReadFromJsonAsync<ProgramDto>(
            JsonOptions,
            cancellationToken
        );
        return new CreateProgramSucceeded(ToSummary(dto!));
    }

    /// <inheritdoc/>
    public async Task<RenameProgramOutcome> RenameProgramAsync(
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

        if (!response.IsSuccessStatusCode)
        {
            return new RenameProgramFailed(await ReadErrorAsync(response, cancellationToken));
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

        return new DeleteProgramFailed(await ReadErrorAsync(response, cancellationToken));
    }

    private static async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        var body = await response.Content.ReadFromJsonAsync<ErrorDto>(
            JsonOptions,
            cancellationToken
        );
        return body?.Error ?? $"Request failed with status {(int)response.StatusCode}.";
    }

    private static ProgramSummary ToSummary(ProgramDto dto) =>
        new(ProgramId.Parse(dto.Id), dto.Name);

    private sealed record ProgramDto(string Id, string Name, string CreatedAt, string UpdatedAt);

    private sealed record ErrorDto(string Error);
}
