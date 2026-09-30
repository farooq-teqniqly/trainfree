namespace Trainfree.Admin.Admin;

/// <summary>A processed image file the admin has chosen but not yet uploaded.</summary>
internal sealed record StagedImage : ExerciseImage
{
    /// <summary>Gets the processed image bytes that will be uploaded.</summary>
    public ReadOnlyMemory<byte> Content { get; }

    /// <summary>Gets the image's MIME type, e.g. <c>image/png</c>.</summary>
    public string ContentType { get; }

    /// <summary>Initializes a new instance of <see cref="StagedImage"/>.</summary>
    /// <param name="content">The processed image bytes.</param>
    /// <param name="contentType">The image's MIME type.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="content"/> or <paramref name="contentType"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="content"/> is empty or <paramref name="contentType"/> is
    /// empty or whitespace.
    /// </exception>
    public StagedImage(byte[] content, string contentType)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length == 0)
        {
            throw new ArgumentException("Image content must not be empty.", nameof(content));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        Content = content;
        ContentType = contentType;
    }
}
