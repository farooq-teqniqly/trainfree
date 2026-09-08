namespace Trainfree.Domain.ProgramExercises;

/// <summary>
/// How many sets are prescribed and how much rest separates them -- the set/rest pairing
/// every program exercise carries regardless of whether it's prescribed by reps or
/// duration.
/// </summary>
public sealed class SetPrescription : IEquatable<SetPrescription>
{
    /// <summary>The number of sets prescribed. Always strictly positive.</summary>
    public int Sets { get; }

    /// <summary>The rest between sets, in seconds. Always strictly positive.</summary>
    public int RestSeconds { get; }

    /// <summary>Initializes a new instance of <see cref="SetPrescription"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sets"/> or <paramref name="restSeconds"/> is not
    /// strictly positive.
    /// </exception>
    public SetPrescription(int sets, int restSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sets, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(restSeconds, 0);

        Sets = sets;
        RestSeconds = restSeconds;
    }

    /// <inheritdoc/>
    public bool Equals(SetPrescription? other) =>
        other is not null && Sets == other.Sets && RestSeconds == other.RestSeconds;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as SetPrescription);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Sets, RestSeconds);

    /// <summary>Determines whether two <see cref="SetPrescription"/> instances are equal.</summary>
    public static bool operator ==(SetPrescription? left, SetPrescription? right) =>
        left?.Equals(right) ?? right is null;

    /// <summary>Determines whether two <see cref="SetPrescription"/> instances are not equal.</summary>
    public static bool operator !=(SetPrescription? left, SetPrescription? right) =>
        !(left == right);
}
