using Trainfree.Domain.Ids;

namespace Trainfree.Domain.ProgramExercises;

/// <summary>
/// A program exercise prescribed by a rep count -- as opposed to
/// <see cref="TimedProgramExercise"/>, which is prescribed by a duration.
/// </summary>
public sealed class RepsProgramExercise : IProgramExercise, IEquatable<RepsProgramExercise>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="prescription"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="reps"/> is not strictly positive, or when
    /// <paramref name="weight"/> is negative.
    /// </exception>
    public RepsProgramExercise(
        ProgramExerciseId id,
        SessionPhaseId sessionPhaseId,
        ExerciseId exerciseId,
        int reps,
        decimal weight,
        SetPrescription prescription,
        ProgramExerciseSide side
    )
    {
        ArgumentNullException.ThrowIfNull(prescription);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(reps, 0);
        ArgumentOutOfRangeException.ThrowIfNegative(weight);

        Id = id;
        SessionPhaseId = sessionPhaseId;
        ExerciseId = exerciseId;
        Reps = reps;
        Weight = weight;
        Sets = prescription.Sets;
        RestSeconds = prescription.RestSeconds;
        Side = side;
    }

    /// <inheritdoc/>
    public bool Equals(RepsProgramExercise? other) =>
        other is not null
        && Id.Equals(other.Id)
        && SessionPhaseId.Equals(other.SessionPhaseId)
        && ExerciseId.Equals(other.ExerciseId)
        && Reps == other.Reps
        && Weight == other.Weight
        && Sets == other.Sets
        && RestSeconds == other.RestSeconds
        && Side == other.Side;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as RepsProgramExercise);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(
            Id,
            SessionPhaseId,
            ExerciseId,
            Reps,
            Weight,
            Sets,
            HashCode.Combine(RestSeconds, Side)
        );

    /// <summary>Determines whether two <see cref="RepsProgramExercise"/> instances are equal.</summary>
    public static bool operator ==(RepsProgramExercise? left, RepsProgramExercise? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Determines whether two <see cref="RepsProgramExercise"/> instances are not equal.</summary>
    public static bool operator !=(RepsProgramExercise? left, RepsProgramExercise? right) =>
        !(left == right);
}
