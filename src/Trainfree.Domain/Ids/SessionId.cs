namespace Trainfree.Domain.Ids;

/// <summary>
/// Strongly-typed identifier for a <c>Session</c>. IDs are always assigned by the
/// Worker API and arrive as strings in API responses -- this type parses and displays
/// them but never generates one.
/// </summary>
public readonly record struct SessionId
{
    private const string Prefix = "SNN-";

    /// <summary>The raw string value of this identifier.</summary>
    public string Value { get; }

    private SessionId(string value) => Value = value;

    /// <summary>Parses <paramref name="value"/> as a <see cref="SessionId"/>.</summary>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is ill-formed.</exception>
    public static SessionId Parse(string value) =>
        TryParse(value, out var id)
            ? id
            : throw new FormatException($"Invalid SessionId: '{value}'.");

    /// <summary>
    /// Attempts to parse <paramref name="value"/> as a <see cref="SessionId"/>.
    /// Returns <see langword="false"/> when the value is ill-formed.
    /// </summary>
    public static bool TryParse(string? value, out SessionId id)
    {
        if (DomainId.IsValid(value, Prefix))
        {
            id = new SessionId(value!);
            return true;
        }

        id = default;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
