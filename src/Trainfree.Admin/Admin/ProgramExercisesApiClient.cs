using System.Net;
using System.Net.Http.Json;
using Trainfree.ApiClients;
using Trainfree.Domain.Ids;
using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="IProgramExercisesApiClient"/>
internal sealed class ProgramExercisesApiClient : ApiClientBase, IProgramExercisesApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProgramExercisesApiClient> _logger;

    public ProgramExercisesApiClient(
        HttpClient httpClient,
        ILogger<ProgramExercisesApiClient> logger
    )
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<IProgramExercise>> GetProgramExercisesAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId,
        CancellationToken cancellationToken = default
    )
    {
        var dtos = await _httpClient.GetFromJsonAsync<List<ProgramExerciseDto>>(
            ExercisesUrl(programId, sessionId, sessionPhaseId),
            JsonOptions,
            cancellationToken
        );

        return dtos?.ConvertAll(ToDomain) ?? [];
    }

    /// <inheritdoc/>
    public Task<CreateProgramExerciseOutcome> CreateRepsProgramExerciseAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId,
        ExerciseId exerciseId,
        int reps,
        int sets,
        int restSeconds,
        CancellationToken cancellationToken = default
    ) =>
        CreateAsync(
            programId,
            sessionId,
            sessionPhaseId,
            new
            {
                exerciseId = exerciseId.Value,
                type = "Reps",
                reps,
                sets,
                restSeconds,
            },
            cancellationToken
        );

    /// <inheritdoc/>
    public Task<CreateProgramExerciseOutcome> CreateTimedProgramExerciseAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId,
        ExerciseId exerciseId,
        int durationSeconds,
        int sets,
        int restSeconds,
        CancellationToken cancellationToken = default
    ) =>
        CreateAsync(
            programId,
            sessionId,
            sessionPhaseId,
            new
            {
                exerciseId = exerciseId.Value,
                type = "Timed",
                durationSeconds,
                sets,
                restSeconds,
            },
            cancellationToken
        );

    private Task<CreateProgramExerciseOutcome> CreateAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId,
        object body,
        CancellationToken cancellationToken
    ) =>
        ExecuteAsync<CreateProgramExerciseOutcome>(
            async () =>
            {
                var response = await _httpClient.PostAsJsonAsync(
                    ExercisesUrl(programId, sessionId, sessionPhaseId),
                    body,
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    return new CreateProgramExerciseFailed(
                        await ReadErrorAsync(response, _logger, cancellationToken)
                    );
                }

                var dto = await response.Content.ReadFromJsonAsync<ProgramExerciseDto>(
                    JsonOptions,
                    cancellationToken
                );
                return dto is null
                    ? new CreateProgramExerciseFailed("Server returned an empty response.")
                    : new CreateProgramExerciseSucceeded(ToDomain(dto));
            },
            error => new CreateProgramExerciseFailed(error),
            "Could not add exercise to phase. Try again.",
            _logger
        );

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="update"/> is <see langword="null"/>.</exception>
    public Task<UpdateProgramExerciseOutcome> UpdateProgramExerciseAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId,
        ProgramExerciseId id,
        ProgramExerciseUpdate update,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(update);

        return ExecuteAsync<UpdateProgramExerciseOutcome>(
            async () =>
            {
                var body = new Dictionary<string, object>();
                if (update.Reps is { } reps)
                {
                    body["reps"] = reps;
                }
                if (update.DurationSeconds is { } durationSeconds)
                {
                    body["durationSeconds"] = durationSeconds;
                }
                if (update.Weight is { } weight)
                {
                    body["weight"] = weight;
                }
                if (update.Sets is { } sets)
                {
                    body["sets"] = sets;
                }
                if (update.RestSeconds is { } restSeconds)
                {
                    body["restSeconds"] = restSeconds;
                }
                if (update.Side is { } side)
                {
                    body["side"] = side.ToString();
                }

                var response = await _httpClient.PatchAsJsonAsync(
                    $"{ExercisesUrl(programId, sessionId, sessionPhaseId)}/{id}",
                    body,
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    return new UpdateProgramExerciseFailed(
                        await ReadErrorAsync(response, _logger, cancellationToken)
                    );
                }

                var dto = await response.Content.ReadFromJsonAsync<ProgramExerciseDto>(
                    JsonOptions,
                    cancellationToken
                );
                return dto is null
                    ? new UpdateProgramExerciseFailed("Server returned an empty response.")
                    : new UpdateProgramExerciseSucceeded(ToDomain(dto));
            },
            error => new UpdateProgramExerciseFailed(error),
            "Could not update exercise. Try again.",
            _logger
        );
    }

    /// <inheritdoc/>
    public Task<DeleteProgramExerciseOutcome> DeleteProgramExerciseAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId,
        ProgramExerciseId id,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteAsync<DeleteProgramExerciseOutcome>(
            async () =>
            {
                var response = await _httpClient.DeleteAsync(
                    new Uri(
                        $"{ExercisesUrl(programId, sessionId, sessionPhaseId)}/{id}",
                        UriKind.Relative
                    ),
                    cancellationToken
                );

                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new DeleteProgramExerciseSucceeded();
                }

                return new DeleteProgramExerciseFailed(
                    await ReadErrorAsync(response, _logger, cancellationToken)
                );
            },
            error => new DeleteProgramExerciseFailed(error),
            "Could not delete exercise. Try again.",
            _logger
        );

    private static string ExercisesUrl(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId
    ) => $"programs/{programId}/sessions/{sessionId}/phases/{sessionPhaseId}/exercises";

    private static IProgramExercise ToDomain(ProgramExerciseDto dto)
    {
        var id = ProgramExerciseId.Parse(dto.Id);
        var sessionPhaseId = SessionPhaseId.Parse(dto.SessionPhaseId);
        var exerciseId = ExerciseId.Parse(dto.ExerciseId);
        var side = Enum.Parse<ProgramExerciseSide>(dto.Side);

        return dto.Type switch
        {
            "Reps" => new RepsProgramExercise(
                id,
                sessionPhaseId,
                exerciseId,
                dto.Reps!.Value,
                dto.Weight,
                dto.Sets,
                dto.RestSeconds,
                side
            ),
            "Timed" => new TimedProgramExercise(
                id,
                sessionPhaseId,
                exerciseId,
                dto.DurationSeconds!.Value,
                dto.Weight,
                dto.Sets,
                dto.RestSeconds,
                side
            ),
            _ => throw new FormatException($"Unknown program exercise type: '{dto.Type}'."),
        };
    }

    private sealed record ProgramExerciseDto(
        string Id,
        string SessionPhaseId,
        string ExerciseId,
        string Type,
        int? Reps,
        int? DurationSeconds,
        decimal Weight,
        int Sets,
        int RestSeconds,
        string Side
    );
}
