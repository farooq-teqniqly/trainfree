using Trainfree.Admin.Admin;

namespace Trainfree.Admin.Tests.Admin;

public sealed class StagedImageTests
{
    [Fact]
    public void Constructor_ValidArguments_ExposesContentAndContentType()
    {
        // Arrange
        byte[] content = [1, 2, 3];

        // Act
        var image = new StagedImage(content, "image/png");

        // Assert
        Assert.IsAssignableFrom<ExerciseImage>(image);
        Assert.Equal(content, image.Content.ToArray());
        Assert.Equal("image/png", image.ContentType);
    }

    [Fact]
    public void Constructor_ContentIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new StagedImage(null!, "image/png"));
    }

    [Fact]
    public void Constructor_ContentIsEmpty_ThrowsArgumentException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentException>(() => new StagedImage([], "image/png"));
    }

    [Fact]
    public void Constructor_ContentTypeIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new StagedImage([1], null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ContentTypeIsEmptyOrWhiteSpace_ThrowsArgumentException(
        string contentType
    )
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentException>(() => new StagedImage([1], contentType));
    }
}
