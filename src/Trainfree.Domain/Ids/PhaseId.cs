namespace Trainfree.Domain.Ids;

/// <summary>
/// Strongly-typed identifier for a <c>Phase</c>. IDs are always assigned by the
/// Worker API and arrive as strings in API responses -- this type parses and displays
/// them but never generates one.
/// </summary>
public readonly record struct PhaseId
{
    private const string Prefix = "PHS-";

    /// <summary>The raw string value of this identifier.</summary>
    public string Value { get; }

    private PhaseId(string value) => Value = value;

    /// <summary>Parses <paramref name="value"/> as a <see cref="PhaseId"/>.</summary>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is ill-formed.</exception>
    public static PhaseId Parse(string value) =>
        TryParse(value, out var id)
            ? id
            : throw new FormatException($"Invalid PhaseId: '{value}'.");

    /// <summary>
    /// Attempts to parse <paramref name="value"/> as a <see cref="PhaseId"/>.
    /// Returns <see langword="false"/> when the value is ill-formed.
    /// </summary>
    public static bool TryParse(string? value, out PhaseId id)
    {
        if (DomainId.IsValid(value, Prefix))
        {
            id = new PhaseId(value!);
            return true;
        }

        id = default;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
