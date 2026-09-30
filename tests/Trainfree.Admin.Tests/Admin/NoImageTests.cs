using Trainfree.Admin.Admin;

namespace Trainfree.Admin.Tests.Admin;

public sealed class NoImageTests
{
    [Fact]
    public void Constructor_Default_IsExerciseImage()
    {
        // Arrange / Act
        var image = new NoImage();

        // Assert
        Assert.IsType<ExerciseImage>(image, exactMatch: false);
    }
}
