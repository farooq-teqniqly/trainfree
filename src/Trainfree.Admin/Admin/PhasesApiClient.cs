using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="IPhasesApiClient"/>
internal sealed partial class PhasesApiClient : IPhasesApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

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
            return new CreatePhaseFailed(await ReadErrorAsync(response, cancellationToken));
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
            return new RenamePhaseFailed(await ReadErrorAsync(response, cancellationToken));
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

        return new DeletePhaseFailed(await ReadErrorAsync(response, cancellationToken));
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

    private static PhaseSummary ToSummary(PhaseDto dto) => new(PhaseId.Parse(dto.Id), dto.Name);

    private sealed record PhaseDto(string Id, string Name, string CreatedAt, string UpdatedAt);

    private sealed record ErrorDto(string Error);
}
