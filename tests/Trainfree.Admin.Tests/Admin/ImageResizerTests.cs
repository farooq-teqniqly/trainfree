using Microsoft.JSInterop;
using NSubstitute;
using Trainfree.Admin.Admin;

namespace Trainfree.Admin.Tests.Admin;

public sealed class ImageResizerTests
{
    private readonly IJSRuntime _jsRuntime = Substitute.For<IJSRuntime>();
    private readonly IJSObjectReference _module = Substitute.For<IJSObjectReference>();

    public ImageResizerTests()
    {
        _jsRuntime
            .InvokeAsync<IJSObjectReference>(
                "import",
                Arg.Any<CancellationToken>(),
                Arg.Is<object?[]?>(a => a != null && (string?)a[0] == "./js/image-resize.js")
            )
            .Returns(new ValueTask<IJSObjectReference>(_module));
    }

    [Fact]
    public async Task ResizeAsync_ModuleReturnsBytes_ReturnsStagedImageWithThoseBytesAndContentType()
    {
        // Arrange
        byte[] input = [1, 2, 3];
        byte[] resized = [9, 8];
        _module
            .InvokeAsync<byte[]>(
                "resizeImage",
                Arg.Any<CancellationToken>(),
                Arg.Is<object?[]?>(a =>
                    a != null && ReferenceEquals(a[0], input) && (string?)a[1] == "image/png"
                )
            )
            .Returns(new ValueTask<byte[]>(resized));
        var resizer = new ImageResizer(_jsRuntime);

        // Act
        var staged = await resizer.ResizeAsync(input, "image/png", CancellationToken.None);

        // Assert
        Assert.Equal(resized, staged.Content.ToArray());
        Assert.Equal("image/png", staged.ContentType);
    }

    [Fact]
    public async Task ResizeAsync_ContentIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var resizer = new ImageResizer(_jsRuntime);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            resizer.ResizeAsync(null!, "image/png", CancellationToken.None)
        );
    }

    [Fact]
    public async Task ResizeAsync_ContentTypeIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var resizer = new ImageResizer(_jsRuntime);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            resizer.ResizeAsync([1], null!, CancellationToken.None)
        );
    }
}
