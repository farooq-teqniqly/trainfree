namespace Trainfree.Domain.Ids;

/// <summary>
/// Strongly-typed identifier for an <c>Exercise</c>. IDs are always assigned by the
/// Worker API and arrive as strings in API responses -- this type parses and displays
/// them but never generates one.
/// </summary>
public readonly record struct ExerciseId
{
    private const string Prefix = "EXR-";

    /// <summary>The raw string value of this identifier.</summary>
    public string Value { get; }

    private ExerciseId(string value) => Value = value;

    /// <summary>Parses <paramref name="value"/> as an <see cref="ExerciseId"/>.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is ill-formed.</exception>
    public static ExerciseId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out var id)
            ? id
            : throw new FormatException($"Invalid ExerciseId: '{value}'.");
    }

    /// <summary>
    /// Attempts to parse <paramref name="value"/> as an <see cref="ExerciseId"/>.
    /// Returns <see langword="false"/> when the value is ill-formed.
    /// </summary>
    public static bool TryParse(string? value, out ExerciseId id)
    {
        if (DomainId.IsValid(value, Prefix))
        {
            id = new ExerciseId(value!);
            return true;
        }

        id = default;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
