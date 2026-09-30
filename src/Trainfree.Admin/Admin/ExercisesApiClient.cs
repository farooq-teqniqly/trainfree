using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Trainfree.ApiClients;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="IExercisesApiClient"/>
internal sealed class ExercisesApiClient : ApiClientBase, IExercisesApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExercisesApiClient> _logger;

    public ExercisesApiClient(HttpClient httpClient, ILogger<ExercisesApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExerciseSummary>> GetExercisesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var dtos = await _httpClient.GetFromJsonAsync<List<ExerciseDto>>(
            "exercises",
            JsonOptions,
            cancellationToken
        );

        return dtos?.ConvertAll(ToSummary) ?? [];
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public Task<CreateExerciseOutcome> CreateExerciseAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return ExecuteAsync<CreateExerciseOutcome>(
            async () =>
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "exercises",
                    new { name },
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    return new CreateExerciseFailed(
                        await ReadErrorAsync(response, _logger, cancellationToken)
                    );
                }

                var dto = await response.Content.ReadFromJsonAsync<ExerciseDto>(
                    JsonOptions,
                    cancellationToken
                );

                return dto is null
                    ? new CreateExerciseFailed("Server returned an empty response.")
                    : new CreateExerciseSucceeded(ToSummary(dto));
            },
            error => new CreateExerciseFailed(error),
            "Could not create exercise. Try again.",
            _logger
        );
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public Task<RenameExerciseOutcome> RenameExerciseAsync(
        ExerciseId id,
        string name,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return ExecuteAsync<RenameExerciseOutcome>(
            async () =>
            {
                var response = await _httpClient.PatchAsJsonAsync(
                    $"exercises/{id}",
                    new { name },
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    return new RenameExerciseFailed(
                        await ReadErrorAsync(response, _logger, cancellationToken)
                    );
                }

                var dto = await response.Content.ReadFromJsonAsync<ExerciseDto>(
                    JsonOptions,
                    cancellationToken
                );

                return dto is null
                    ? new RenameExerciseFailed("Server returned an empty response.")
                    : new RenameExerciseSucceeded(ToSummary(dto));
            },
            error => new RenameExerciseFailed(error),
            "Could not rename exercise. Try again.",
            _logger
        );
    }

    /// <inheritdoc/>
    public Task<DeleteExerciseOutcome> DeleteExerciseAsync(
        ExerciseId id,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteAsync<DeleteExerciseOutcome>(
            async () =>
            {
                var response = await _httpClient.DeleteAsync(
                    new Uri($"exercises/{id}", UriKind.Relative),
                    cancellationToken
                );

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new DeleteExerciseSucceeded();
                }

                return new DeleteExerciseFailed(
                    await ReadErrorAsync(response, _logger, cancellationToken)
                );
            },
            error => new DeleteExerciseFailed(error),
            "Could not delete exercise. Try again.",
            _logger
        );

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="image"/> is <see langword="null"/>.</exception>
    public Task<UploadExerciseImageOutcome> UploadExerciseImageAsync(
        ExerciseId id,
        StagedImage image,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(image);

        return ExecuteAsync<UploadExerciseImageOutcome>(
            async () =>
            {
                using var content = new ByteArrayContent(image.Content.ToArray());
                content.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);

                var response = await _httpClient.PutAsync(
                    new Uri($"exercises/{id}/image", UriKind.Relative),
                    content,
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    return new UploadExerciseImageFailed(
                        await ReadErrorAsync(response, _logger, cancellationToken)
                    );
                }

                var dto = await response.Content.ReadFromJsonAsync<ExerciseDto>(
                    JsonOptions,
                    cancellationToken
                );

                return dto is null
                    ? new UploadExerciseImageFailed("Server returned an empty response.")
                    : new UploadExerciseImageSucceeded(ToSummary(dto));
            },
            error => new UploadExerciseImageFailed(error),
            "Could not upload image. Try again.",
            _logger
        );
    }

    /// <inheritdoc/>
    public Task<DeleteExerciseImageOutcome> DeleteExerciseImageAsync(
        ExerciseId id,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteAsync<DeleteExerciseImageOutcome>(
            async () =>
            {
                var response = await _httpClient.DeleteAsync(
                    new Uri($"exercises/{id}/image", UriKind.Relative),
                    cancellationToken
                );

                return
                    response.IsSuccessStatusCode || response.StatusCode is HttpStatusCode.NotFound
                    ? new DeleteExerciseImageSucceeded()
                    : new DeleteExerciseImageFailed(
                        await ReadErrorAsync(response, _logger, cancellationToken)
                    );
            },
            error => new DeleteExerciseImageFailed(error),
            "Could not delete image. Try again.",
            _logger
        );

    private static ExerciseSummary ToSummary(ExerciseDto dto) =>
        new(ExerciseId.Parse(dto.Id), dto.Name, dto.ImageUrl);

    private sealed record ExerciseDto(
        string Id,
        string Name,
        string? ImageUrl,
        string CreatedAt,
        string UpdatedAt
    );
}
