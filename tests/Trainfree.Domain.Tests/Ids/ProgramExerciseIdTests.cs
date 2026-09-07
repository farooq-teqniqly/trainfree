using Trainfree.Domain.Ids;

namespace Trainfree.Domain.Tests.Ids;

public sealed class ProgramExerciseIdTests
{
    [Theory]
    [InlineData("PGX-7K2QXM")]
    [InlineData("PGX-234567")]
    [InlineData("PGX-ABCDEF")]
    public void TryParse_WellFormedValue_ReturnsTrueAndParsedId(string value)
    {
        // Act
        var result = ProgramExerciseId.TryParse(value, out var id);

        // Assert
        Assert.True(result);
        Assert.Equal(value, id.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PGX-7K2QX")]
    [InlineData("PGX-7K2QXMM")]
    [InlineData("PGX-7K2Q0M")]
    [InlineData("PGX-7K2Q1M")]
    [InlineData("PGX-7K2QOM")]
    [InlineData("PGX-7K2QIM")]
    [InlineData("PGX-7K2QLM")]
    [InlineData("pgx-7K2QXM")]
    [InlineData("XYZ-7K2QXM")]
    [InlineData("PGX7K2QXM")]
    public void TryParse_IllFormedValue_ReturnsFalse(string? value)
    {
        // Act
        var result = ProgramExerciseId.TryParse(value, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Parse_WellFormedValue_ReturnsParsedId()
    {
        // Act
        var id = ProgramExerciseId.Parse("PGX-7K2QXM");

        // Assert
        Assert.Equal("PGX-7K2QXM", id.ToString());
    }

    [Fact]
    public void Parse_IllFormedValue_ThrowsFormatException()
    {
        // Act / Assert
        Assert.Throws<FormatException>(() => ProgramExerciseId.Parse("not-an-id"));
    }

    [Fact]
    public void Parse_NullValue_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => ProgramExerciseId.Parse(null!));
    }

    [Fact]
    public void ToString_ParsedValue_RoundTripsOriginalValue()
    {
        // Arrange
        const string value = "PGX-7K2QXM";
        var id = ProgramExerciseId.Parse(value);

        // Act
        var result = id.ToString();

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var first = ProgramExerciseId.Parse("PGX-7K2QXM");
        var second = ProgramExerciseId.Parse("PGX-7K2QXM");

        // Act
        var result = first.Equals(second);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var first = ProgramExerciseId.Parse("PGX-7K2QXM");
        var second = ProgramExerciseId.Parse("PGX-234567");

        // Act
        var result = first.Equals(second);

        // Assert
        Assert.False(result);
    }
}
