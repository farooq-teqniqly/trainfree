using System.Text;

namespace Trainfree.Domain.Users;

/// <summary>
/// An email address identifying a signed-in user. Wraps a single string whose only
/// invariant is that it is not blank; the address arrives already validated by
/// Cloudflare Access, so this type does not re-implement address syntax.
/// </summary>
public readonly record struct EmailAddress
{
    /// <summary>
    /// The address, trimmed of surrounding whitespace; empty only for
    /// <see langword="default"/>, which is not a valid address.
    /// </summary>
    public string Value => _value ?? string.Empty;

    /// <summary>
    /// The first character of the address, uppercased, for use as a compact identifier such
    /// as an avatar label. A surrogate pair is kept whole rather than split.
    /// </summary>
    public string Initial =>
        Value.Length == 0
            ? string.Empty
            : Rune.ToUpperInvariant(Value.EnumerateRunes().First()).ToString();

    private readonly string? _value;

    private EmailAddress(string value) => _value = value;

    /// <summary>
    /// Creates an <see cref="EmailAddress"/> from <paramref name="value"/>, trimming surrounding
    /// whitespace so the initial is never a blank.
    /// </summary>
    /// <param name="value">The address to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or whitespace.</exception>
    public static EmailAddress Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new EmailAddress(value.Trim());
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
