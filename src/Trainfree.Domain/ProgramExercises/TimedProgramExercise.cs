using Trainfree.Domain.Ids;

namespace Trainfree.Domain.ProgramExercises;

/// <summary>
/// A program exercise prescribed by a duration -- as opposed to
/// <see cref="RepsProgramExercise"/>, which is prescribed by a rep count.
/// </summary>
public sealed class TimedProgramExercise : IProgramExercise
{
    /// <inheritdoc/>
    public ProgramExerciseId Id { get; }

    /// <inheritdoc/>
    public SessionPhaseId SessionPhaseId { get; }

    /// <inheritdoc/>
    public ExerciseId ExerciseId { get; }

    /// <summary>The duration prescribed per set, in seconds. Always strictly positive.</summary>
    public int DurationSeconds { get; }

    /// <inheritdoc/>
    public decimal Weight { get; }

    /// <inheritdoc/>
    public int Sets { get; }

    /// <inheritdoc/>
    public int RestSeconds { get; }

    /// <inheritdoc/>
    public ProgramExerciseSide Side { get; }

    /// <summary>Initializes a new instance of <see cref="TimedProgramExercise"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="durationSeconds"/>, <paramref name="sets"/>, or
    /// <paramref name="restSeconds"/> is not strictly positive, or when
    /// <paramref name="weight"/> is negative.
    /// </exception>
    public TimedProgramExercise(
        ProgramExerciseId id,
        SessionPhaseId sessionPhaseId,
        ExerciseId exerciseId,
        int durationSeconds,
        decimal weight,
        int sets,
        int restSeconds,
        ProgramExerciseSide side
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(durationSeconds, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(weight);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sets, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(restSeconds, 0);

        Id = id;
        SessionPhaseId = sessionPhaseId;
        ExerciseId = exerciseId;
        DurationSeconds = durationSeconds;
        Weight = weight;
        Sets = sets;
        RestSeconds = restSeconds;
        Side = side;
    }
}
