using Trainfree.Domain.Ids;
using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Admin;

/// <summary>
/// Shared wire DTO and mapping for a program exercise, used by every API client that
/// deserializes a program exercise row (<see cref="ProgramExercisesApiClient"/> and
/// <see cref="ProgramTreeApiClient"/>).
/// </summary>
internal static class ProgramExerciseMapping
{
    /// <summary>
    /// The wire shape of a program exercise row.
    /// </summary>
    /// <param name="Id">The program exercise's identifier.</param>
    /// <param name="SessionPhaseId">The owning session phase's identifier.</param>
    /// <param name="ExerciseId">The exercise library entry's identifier.</param>
    /// <param name="Type">Either "Reps" or "Timed".</param>
    /// <param name="Reps">The rep count, set only when <paramref name="Type"/> is "Reps".</param>
    /// <param name="DurationSeconds">The duration in seconds, set only when <paramref name="Type"/> is "Timed".</param>
    /// <param name="Weight">The weight used.</param>
    /// <param name="Sets">The number of sets.</param>
    /// <param name="RestSeconds">The rest between sets, in seconds.</param>
    /// <param name="Side">Which side of the body the exercise targets.</param>
    internal sealed record ProgramExerciseDto(
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

    /// <summary>
    /// Maps a <see cref="ProgramExerciseDto"/> to its domain type.
    /// </summary>
    /// <param name="dto">The DTO to map.</param>
    /// <returns>The mapped <see cref="IProgramExercise"/>.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="dto"/>'s <see cref="ProgramExerciseDto.Type"/> is neither "Reps" nor "Timed".</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="dto"/>'s <see cref="ProgramExerciseDto.Side"/> does not parse as a <see cref="ProgramExerciseSide"/>.</exception>
    internal static IProgramExercise ToDomain(ProgramExerciseDto dto)
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
}
