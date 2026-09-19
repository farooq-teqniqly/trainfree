using System.Text;

namespace Trainfree.Domain.Users;

/// <summary>
/// An email address identifying a signed-in user. Wraps a single string whose only
/// invariant is that it is not blank; the address arrives already validated by
/// Cloudflare Access, so this type does not re-implement address syntax.
/// </summary>
public readonly record struct EmailAddress
{
    /// <summary>The address exactly as issued by the identity provider.</summary>
    public string Value { get; }

    /// <summary>
    /// The first character of the address, uppercased, for use as a compact identifier such
    /// as an avatar label. A surrogate pair is kept whole rather than split.
    /// </summary>
    public string Initial => Rune.ToUpperInvariant(Value.EnumerateRunes().First()).ToString();

    private EmailAddress(string value) => Value = value;

    /// <summary>Creates an <see cref="EmailAddress"/> from <paramref name="value"/>.</summary>
    /// <param name="value">The address to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or whitespace.</exception>
    public static EmailAddress Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new EmailAddress(value);
    }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
