namespace Trainfree.Web.Ids;

/// <summary>
/// Validates Crockford base32 character sequences.
/// Alphabet: uppercase A-Z and 2-9, excluding the ambiguous characters 0, O, 1, I, L.
/// </summary>
internal static class CrockfordBase32
{
    /// <summary>The valid Crockford base32 alphabet used by domain IDs.</summary>
    internal const string Alphabet = "ABCDEFGHJKMNPQRSTVWXYZ23456789";

    /// <summary>
    /// Returns <see langword="true"/> if every character in <paramref name="body"/> is a
    /// valid Crockford base32 character.
    /// </summary>
    internal static bool IsValidBody(ReadOnlySpan<char> body)
    {
        foreach (var c in body)
        {
            if (!Alphabet.Contains(c, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
