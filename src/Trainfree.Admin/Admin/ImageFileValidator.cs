namespace Trainfree.Admin.Admin;

/// <summary>
/// Gives immediate feedback on a picked file's type and size. The Worker re-validates
/// authoritatively; this check only avoids decoding and uploading a file it would refuse.
/// </summary>
internal static class ImageFileValidator
{
    /// <summary>The largest accepted file, in bytes (1 MB).</summary>
    public const long MaxBytes = 1_048_576;

    /// <summary>The number of leading bytes <see cref="ValidateSignature"/> needs.</summary>
    public const int SignatureLength = 8;

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

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

    /// <summary>
    /// Checks that the file's leading bytes are the JPEG or PNG signature that matches the
    /// declared content type. The declared type comes from the file extension, so a renamed
    /// non-image would otherwise only fail later, in the decoder.
    /// </summary>
    /// <param name="header">
    /// The file's first bytes; <see cref="SignatureLength"/> bytes are enough.
    /// </param>
    /// <param name="contentType">The file's declared MIME type.</param>
    /// <returns>
    /// <see cref="ImageFileAccepted"/> when the signature matches the declared type;
    /// otherwise <see cref="ImageFileRejected"/>, including for a header too short to hold
    /// the signature.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="header"/> or <paramref name="contentType"/> is
    /// <see langword="null"/>.
    /// </exception>
    public static ImageFileValidationOutcome ValidateSignature(byte[] header, string contentType)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(contentType);

        var signature = contentType.ToUpperInvariant() switch
        {
            "IMAGE/JPEG" => JpegSignature,
            "IMAGE/PNG" => PngSignature,
            _ => null,
        };

        return signature is not null && header.AsSpan().StartsWith(signature)
            ? new ImageFileAccepted(contentType)
            : new ImageFileRejected("Only JPG and PNG are supported");
    }
}
