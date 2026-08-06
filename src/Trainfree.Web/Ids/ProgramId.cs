namespace Trainfree.Web.Ids;

/// <summary>
/// Strongly-typed identifier for a <c>Program</c>. IDs are always assigned by the
/// Worker API and arrive as strings in API responses -- this type parses and displays
/// them but never generates one.
/// </summary>
internal readonly record struct ProgramId
{
    private const string Prefix = "PRG-";

    /// <summary>The raw string value of this identifier.</summary>
    public string Value { get; }

    private ProgramId(string value) => Value = value;

    /// <summary>Parses <paramref name="value"/> as a <see cref="ProgramId"/>.</summary>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is ill-formed.</exception>
    public static ProgramId Parse(string value) =>
        TryParse(value, out var id)
            ? id
            : throw new FormatException($"Invalid ProgramId: '{value}'.");

    /// <summary>
    /// Attempts to parse <paramref name="value"/> as a <see cref="ProgramId"/>.
    /// Returns <see langword="false"/> when the value is ill-formed.
    /// </summary>
    public static bool TryParse(string value, out ProgramId id)
    {
        if (DomainId.IsValid(value, Prefix))
        {
            id = new ProgramId(value);
            return true;
        }

        id = default;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
