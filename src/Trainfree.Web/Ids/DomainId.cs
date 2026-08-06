namespace Trainfree.Web.Ids;

/// <summary>
/// Internal helpers shared by all domain identifier types.
/// </summary>
internal static class DomainId
{
    /// <summary>Number of Crockford base32 characters in the body of a domain ID.</summary>
    internal const int BodyLength = 6;

    /// <summary>
    /// Validates that <paramref name="value"/> matches <c>PREFIX-BODY</c> where BODY
    /// is exactly <see cref="BodyLength"/> valid Crockford base32 characters.
    /// </summary>
    internal static bool IsValid(string? value, string prefix)
    {
        if (value is null)
        {
            return false;
        }

        var expectedLength = prefix.Length + BodyLength;
        if (value.Length != expectedLength)
        {
            return false;
        }

        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return CrockfordBase32.IsValidBody(value.AsSpan(prefix.Length));
    }
}
