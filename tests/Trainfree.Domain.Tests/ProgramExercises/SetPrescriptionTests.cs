using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Domain.Tests.ProgramExercises;

public sealed class SetPrescriptionTests
{
    [Fact]
    public void Constructor_ValidValues_AssignsProperties()
    {
        // Act
        var result = new SetPrescription(3, 60);

        // Assert
        Assert.Equal(3, result.Sets);
        Assert.Equal(60, result.RestSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveSets_ThrowsArgumentOutOfRangeException(int sets)
    {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SetPrescription(sets, 60));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveRestSeconds_ThrowsArgumentOutOfRangeException(
        int restSeconds
    )
    {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SetPrescription(3, restSeconds));
    }

    [Fact]
    public void Equals_SamePropertyValues_ReturnsTrue()
    {
        // Arrange
        var left = new SetPrescription(3, 60);
        var right = new SetPrescription(3, 60);

        // Act / Assert
        Assert.Equal(left, right);
        Assert.True(left == right);
        Assert.False(left != right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentSets_ReturnsFalse()
    {
        // Arrange
        var left = new SetPrescription(3, 60);
        var right = new SetPrescription(4, 60);

        // Act / Assert
        Assert.NotEqual(left, right);
        Assert.False(left == right);
        Assert.True(left != right);
    }
}
