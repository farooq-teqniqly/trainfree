using Trainfree.Domain.Ids;

namespace Trainfree.Domain.ProgramExercises;

/// <summary>
/// A program exercise prescribed by a rep count -- as opposed to
/// <see cref="TimedProgramExercise"/>, which is prescribed by a duration.
/// </summary>
public sealed class RepsProgramExercise : IProgramExercise
{
    /// <inheritdoc/>
    public ProgramExerciseId Id { get; }

    /// <inheritdoc/>
    public SessionPhaseId SessionPhaseId { get; }

    /// <inheritdoc/>
    public ExerciseId ExerciseId { get; }

    /// <summary>The number of reps prescribed per set. Always strictly positive.</summary>
    public int Reps { get; }

    /// <inheritdoc/>
    public decimal Weight { get; }

    /// <inheritdoc/>
    public int Sets { get; }

    /// <inheritdoc/>
    public int RestSeconds { get; }

    /// <inheritdoc/>
    public ProgramExerciseSide Side { get; }

    /// <summary>Initializes a new instance of <see cref="RepsProgramExercise"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="reps"/>, <paramref name="sets"/>, or
    /// <paramref name="restSeconds"/> is not strictly positive, or when
    /// <paramref name="weight"/> is negative.
    /// </exception>
    public RepsProgramExercise(
        ProgramExerciseId id,
        SessionPhaseId sessionPhaseId,
        ExerciseId exerciseId,
        int reps,
        decimal weight,
        int sets,
        int restSeconds,
        ProgramExerciseSide side
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(reps, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(weight);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sets, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(restSeconds, 0);

        Id = id;
        SessionPhaseId = sessionPhaseId;
        ExerciseId = exerciseId;
        Reps = reps;
        Weight = weight;
        Sets = sets;
        RestSeconds = restSeconds;
        Side = side;
    }
}
