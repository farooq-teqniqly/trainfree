namespace Trainfree.Admin.Admin;

/// <summary>
/// Gives immediate feedback on a picked file's type and size. The Worker re-validates
/// authoritatively; this check only avoids decoding and uploading a file it would refuse.
/// </summary>
internal static class ImageFileValidator
{
    /// <summary>The largest accepted file, in bytes (1 MB).</summary>
    public const long MaxBytes = 1_048_576;

    /// <summary>
    /// Checks the declared content type, then the size. Type is checked first so a
    /// wrong-type file is never reported as merely too large.
    /// </summary>
    /// <param name="contentType">The file's declared MIME type.</param>
    /// <param name="sizeBytes">The file's size in bytes.</param>
    /// <returns>
    /// <see cref="ImageFileAccepted"/> for JPEG or PNG at most <see cref="MaxBytes"/>;
    /// otherwise <see cref="ImageFileRejected"/> with the reason.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="contentType"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="sizeBytes"/> is negative.
    /// </exception>
    public static ImageFileValidationOutcome Validate(string contentType, long sizeBytes)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);

        if (
            !contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
            && !contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
        )
        {
            return new ImageFileRejected("Only JPG and PNG are supported");
        }

        return sizeBytes > MaxBytes
            ? new ImageFileRejected("Image must be 1 MB or smaller")
            : new ImageFileAccepted(contentType);
    }
}
