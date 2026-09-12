using System.Net.Http.Json;
using Trainfree.ApiClients;
using Trainfree.Domain.Ids;
using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Admin;

/// <inheritdoc cref="IProgramTreeApiClient"/>
internal sealed class ProgramTreeApiClient : ApiClientBase, IProgramTreeApiClient
{
    private readonly HttpClient _httpClient;

    public ProgramTreeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProgramTreeItem>> GetProgramTreeAsync(
        CancellationToken cancellationToken = default
    )
    {
        var dtos = await _httpClient.GetFromJsonAsync<List<ProgramTreeDto>>(
            "programs-tree",
            JsonOptions,
            cancellationToken
        );

        return dtos?.ConvertAll(ToDomain) ?? [];
    }

    private static ProgramTreeItem ToDomain(ProgramTreeDto dto) =>
        new(
            new ProgramSummary(ProgramId.Parse(dto.Id), dto.Name),
            dto.Sessions.ConvertAll(ToDomain)
        );

    private static SessionTreeItem ToDomain(SessionTreeDto dto) =>
        new(
            new SessionSummary(SessionId.Parse(dto.Id), ProgramId.Parse(dto.ProgramId), dto.Name),
            dto.Phases.ConvertAll(ToDomain)
        );

    private static SessionPhaseTreeItem ToDomain(SessionPhaseTreeDto dto) =>
        new(
            new SessionPhaseSummary(
                SessionPhaseId.Parse(dto.Id),
                SessionId.Parse(dto.SessionId),
                PhaseId.Parse(dto.PhaseId)
            ),
            dto.Exercises.ConvertAll(ToDomain)
        );

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
                new SetPrescription(dto.Sets, dto.RestSeconds),
                side
            ),
            "Timed" => new TimedProgramExercise(
                id,
                sessionPhaseId,
                exerciseId,
                dto.DurationSeconds!.Value,
                dto.Weight,
                new SetPrescription(dto.Sets, dto.RestSeconds),
                side
            ),
            _ => throw new FormatException($"Unknown program exercise type: '{dto.Type}'."),
        };
    }

    private sealed record ProgramTreeDto(string Id, string Name, List<SessionTreeDto> Sessions);

    private sealed record SessionTreeDto(
        string Id,
        string ProgramId,
        string Name,
        List<SessionPhaseTreeDto> Phases
    );

    private sealed record SessionPhaseTreeDto(
        string Id,
        string SessionId,
        string PhaseId,
        List<ProgramExerciseDto> Exercises
    );

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
