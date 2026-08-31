namespace Trainfree.Domain.Ids;

/// <summary>
/// Strongly-typed identifier for a <c>Category</c>. IDs are always assigned by the
/// Worker API and arrive as strings in API responses -- this type parses and displays
/// them but never generates one.
/// </summary>
public readonly record struct CategoryId
{
    private const string Prefix = "CAT-";

    /// <summary>The raw string value of this identifier.</summary>
    public string Value { get; }

    private CategoryId(string value) => Value = value;

    /// <summary>Parses <paramref name="value"/> as a <see cref="CategoryId"/>.</summary>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is ill-formed.</exception>
    public static CategoryId Parse(string value) =>
        TryParse(value, out var id)
            ? id
            : throw new FormatException($"Invalid CategoryId: '{value}'.");

    /// <summary>
    /// Attempts to parse <paramref name="value"/> as a <see cref="CategoryId"/>.
    /// Returns <see langword="false"/> when the value is ill-formed.
    /// </summary>
    public static bool TryParse(string? value, out CategoryId id)
    {
        if (DomainId.IsValid(value, Prefix))
        {
            id = new CategoryId(value!);
            return true;
        }

        id = default;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
