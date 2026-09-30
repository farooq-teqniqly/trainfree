using Trainfree.Admin.Admin;

namespace Trainfree.Admin.Tests.Admin;

public sealed class ImageFileValidatorTests
{
    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("IMAGE/PNG")]
    public void Validate_SupportedTypeWithinLimit_ReturnsAccepted(string contentType)
    {
        // Arrange / Act
        var outcome = ImageFileValidator.Validate(contentType, 300_000);

        // Assert
        var accepted = Assert.IsType<ImageFileAccepted>(outcome);
        Assert.Equal(contentType, accepted.ContentType);
    }

    [Fact]
    public void Validate_SizeExactlyAtLimit_ReturnsAccepted()
    {
        // Arrange / Act
        var outcome = ImageFileValidator.Validate("image/png", 1_048_576);

        // Assert
        Assert.IsType<ImageFileAccepted>(outcome);
    }

    [Fact]
    public void Validate_SizeOneByteOverLimit_ReturnsRejectedWithSizeError()
    {
        // Arrange / Act
        var outcome = ImageFileValidator.Validate("image/png", 1_048_577);

        // Assert
        var rejected = Assert.IsType<ImageFileRejected>(outcome);
        Assert.Equal("Image must be 1 MB or smaller", rejected.Error);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    [InlineData("image/svg+xml")]
    [InlineData("")]
    public void Validate_UnsupportedType_ReturnsRejectedWithTypeError(string contentType)
    {
        // Arrange / Act
        var outcome = ImageFileValidator.Validate(contentType, 100);

        // Assert
        var rejected = Assert.IsType<ImageFileRejected>(outcome);
        Assert.Equal("Only JPG and PNG are supported", rejected.Error);
    }

    [Fact]
    public void Validate_UnsupportedTypeAndOversize_ReturnsTypeError()
    {
        // Arrange / Act
        var outcome = ImageFileValidator.Validate("image/gif", 5_000_000);

        // Assert
        var rejected = Assert.IsType<ImageFileRejected>(outcome);
        Assert.Equal("Only JPG and PNG are supported", rejected.Error);
    }

    [Fact]
    public void Validate_ContentTypeIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => ImageFileValidator.Validate(null!, 1));
    }

    [Theory]
    [InlineData("jpeg", "image/jpeg")]
    [InlineData("jpeg", "IMAGE/JPEG")]
    [InlineData("png", "image/png")]
    public void ValidateSignature_SignatureMatchesDeclaredType_ReturnsAccepted(
        string kind,
        string contentType
    )
    {
        // Arrange
        var header = HeaderFor(kind);

        // Act
        var outcome = ImageFileValidator.ValidateSignature(header, contentType);

        // Assert
        var accepted = Assert.IsType<ImageFileAccepted>(outcome);
        Assert.Equal(contentType, accepted.ContentType);
    }

    [Theory]
    [InlineData("jpeg", "image/png")]
    [InlineData("png", "image/jpeg")]
    [InlineData("text", "image/png")]
    [InlineData("text", "image/jpeg")]
    [InlineData("short", "image/png")]
    [InlineData("shortjpeg", "image/png")]
    [InlineData("empty", "image/jpeg")]
    [InlineData("jpeg", "image/gif")]
    public void ValidateSignature_SignatureDoesNotMatchDeclaredType_ReturnsRejectedWithTypeError(
        string kind,
        string contentType
    )
    {
        // Arrange
        var header = HeaderFor(kind);

        // Act
        var outcome = ImageFileValidator.ValidateSignature(header, contentType);

        // Assert
        var rejected = Assert.IsType<ImageFileRejected>(outcome);
        Assert.Equal("Only JPG and PNG are supported", rejected.Error);
    }

    [Fact]
    public void ValidateSignature_HeaderIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            ImageFileValidator.ValidateSignature(null!, "image/png")
        );
    }

    [Fact]
    public void ValidateSignature_ContentTypeIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            ImageFileValidator.ValidateSignature(HeaderFor("png"), null!)
        );
    }

    private static byte[] HeaderFor(string kind) =>
        kind switch
        {
            "jpeg" => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46],
            "png" => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
            "text" => "hello wo"u8.ToArray(),
            "short" => [0x89, 0x50, 0x4E, 0x47],
            "shortjpeg" => [0xFF, 0xD8],
            "empty" => [],
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    [Fact]
    public void Validate_NegativeSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImageFileValidator.Validate("image/png", -1)
        );
    }
}
