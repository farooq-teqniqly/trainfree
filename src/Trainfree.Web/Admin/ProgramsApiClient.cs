using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trainfree.Web.Ids;

namespace Trainfree.Web.Admin;

/// <inheritdoc cref="IProgramsApiClient"/>
internal sealed partial class ProgramsApiClient : IProgramsApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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
            LogErrorBodyUnreadable(_logger, ex);
            return fallback;
        }
    }

    private static ProgramSummary ToSummary(ProgramDto dto) =>
        new(ProgramId.Parse(dto.Id), dto.Name);

    private sealed record ProgramDto(string Id, string Name, string CreatedAt, string UpdatedAt);

    private sealed record ErrorDto(string Error);
}
