using Trainfree.Admin.Admin;

namespace Trainfree.Admin.Tests.Admin;

public sealed class HasImageTests
{
    [Fact]
    public void Constructor_ValidUrl_ExposesUrlAndIsExerciseImage()
    {
        // Arrange / Act
        var image = new HasImage("/api/exercises/EXR-AAAAAA/image?v=abc");

        // Assert
        Assert.IsType<ExerciseImage>(image, exactMatch: false);
        Assert.Equal("/api/exercises/EXR-AAAAAA/image?v=abc", image.Url);
    }

    [Fact]
    public void Constructor_UrlIsNull_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new HasImage(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_UrlIsEmptyOrWhiteSpace_ThrowsArgumentException(string path)
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentException>(() => new HasImage(path));
    }
}
