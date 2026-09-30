using Microsoft.JSInterop;

namespace Trainfree.Admin.Admin;

/// <summary>Resizes images through the <c>wwwroot/js/image-resize.js</c> module.</summary>
internal sealed class ImageResizer : IImageResizer
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    /// <summary>Initializes a new instance of <see cref="ImageResizer"/>.</summary>
    /// <param name="jsRuntime">The JavaScript runtime used to load the resize module.</param>
    public ImageResizer(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task<StagedImage> ResizeAsync(
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(contentType);

        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            cancellationToken,
            "./js/image-resize.js"
        );
        var resized = await _module.InvokeAsync<byte[]>(
            "resizeImage",
            cancellationToken,
            content,
            contentType
        );
        return new StagedImage(resized, contentType);
    }
}
