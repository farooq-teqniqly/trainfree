namespace Trainfree.Domain.Ids;

/// <summary>
/// Strongly-typed identifier for a <c>SessionPhase</c>. IDs are always assigned by the
/// Worker API and arrive as strings in API responses -- this type parses and displays
/// them but never generates one.
/// </summary>
public readonly record struct SessionPhaseId
{
    private const string Prefix = "SPH-";

    /// <summary>The raw string value of this identifier.</summary>
    public string Value { get; }

    private SessionPhaseId(string value) => Value = value;

    /// <summary>Parses <paramref name="value"/> as a <see cref="SessionPhaseId"/>.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is ill-formed.</exception>
    public static SessionPhaseId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TryParse(value, out var id)
            ? id
            : throw new FormatException($"Invalid SessionPhaseId: '{value}'.");
    }

    /// <summary>
    /// Attempts to parse <paramref name="value"/> as a <see cref="SessionPhaseId"/>.
    /// Returns <see langword="false"/> when the value is ill-formed.
    /// </summary>
    public static bool TryParse(string? value, out SessionPhaseId id)
    {
        if (DomainId.IsValid(value, Prefix))
        {
            id = new SessionPhaseId(value!);
            return true;
        }

        id = default;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
