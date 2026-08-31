using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="ICategoriesApiClient"/>
internal sealed partial class CategoriesApiClient : ICategoriesApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<CategoriesApiClient> _logger;

    public CategoriesApiClient(HttpClient httpClient, ILogger<CategoriesApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var dtos = await _httpClient.GetFromJsonAsync<List<CategoryDto>>(
            "categories",
            JsonOptions,
            cancellationToken
        );

        return dtos?.ConvertAll(ToSummary) ?? [];
    }

    /// <inheritdoc/>
    public async Task<CreateCategoryOutcome> CreateCategoryAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PostAsJsonAsync(
            "categories",
            new { name },
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return new CreateCategoryFailed(await ReadErrorAsync(response, cancellationToken));
        }

        var dto = await response.Content.ReadFromJsonAsync<CategoryDto>(
            JsonOptions,
            cancellationToken
        );
        return new CreateCategorySucceeded(ToSummary(dto!));
    }

    /// <inheritdoc/>
    public async Task<RenameCategoryOutcome> RenameCategoryAsync(
        CategoryId id,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.PatchAsJsonAsync(
            $"categories/{id}",
            new { name },
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return new RenameCategoryFailed(await ReadErrorAsync(response, cancellationToken));
        }

        var dto = await response.Content.ReadFromJsonAsync<CategoryDto>(
            JsonOptions,
            cancellationToken
        );
        return new RenameCategorySucceeded(ToSummary(dto!));
    }

    /// <inheritdoc/>
    public async Task<DeleteCategoryOutcome> DeleteCategoryAsync(
        CategoryId id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.DeleteAsync(
            new Uri($"categories/{id}", UriKind.Relative),
            cancellationToken
        );

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
        {
            return new DeleteCategorySucceeded();
        }

        return new DeleteCategoryFailed(await ReadErrorAsync(response, cancellationToken));
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

    private static CategorySummary ToSummary(CategoryDto dto) =>
        new(CategoryId.Parse(dto.Id), dto.Name);

    private sealed record CategoryDto(string Id, string Name, string CreatedAt, string UpdatedAt);

    private sealed record ErrorDto(string Error);
}
