namespace Trainfree.Admin.Admin;

/// <summary>Downscales a picked image in the browser before it is staged for upload.</summary>
internal interface IImageResizer
{
    /// <summary>
    /// Scales the image's longer edge down to at most 800 px, never enlarging, and
    /// re-encodes it in its original format.
    /// </summary>
    /// <param name="content">The original image bytes.</param>
    /// <param name="contentType">The image's MIME type, <c>image/jpeg</c> or <c>image/png</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The processed image, ready to upload.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="content"/> or <paramref name="contentType"/> is <see langword="null"/>.
    /// </exception>
    Task<StagedImage> ResizeAsync(
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default
    );
}
