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

    [Fact]
    public void Validate_NegativeSize_ThrowsArgumentOutOfRangeException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImageFileValidator.Validate("image/png", -1)
        );
    }
}
