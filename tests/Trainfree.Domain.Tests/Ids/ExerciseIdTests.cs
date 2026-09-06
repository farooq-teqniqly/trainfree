using Trainfree.Domain.Ids;

namespace Trainfree.Domain.Tests.Ids;

public sealed class ExerciseIdTests
{
    [Theory]
    [InlineData("EXR-7K2QXM")]
    [InlineData("EXR-234567")]
    [InlineData("EXR-ABCDEF")]
    public void TryParse_WellFormedValue_ReturnsTrueAndParsedId(string value)
    {
        // Act
        var result = ExerciseId.TryParse(value, out var id);

        // Assert
        Assert.True(result);
        Assert.Equal(value, id.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EXR-7K2QX")]
    [InlineData("EXR-7K2QXMM")]
    [InlineData("EXR-7K2Q0M")]
    [InlineData("EXR-7K2Q1M")]
    [InlineData("EXR-7K2QOM")]
    [InlineData("EXR-7K2QIM")]
    [InlineData("EXR-7K2QLM")]
    [InlineData("exr-7K2QXM")]
    [InlineData("XYZ-7K2QXM")]
    [InlineData("EXR7K2QXM")]
    public void TryParse_IllFormedValue_ReturnsFalse(string? value)
    {
        // Act
        var result = ExerciseId.TryParse(value, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Parse_WellFormedValue_ReturnsParsedId()
    {
        // Act
        var id = ExerciseId.Parse("EXR-7K2QXM");

        // Assert
        Assert.Equal("EXR-7K2QXM", id.ToString());
    }

    [Fact]
    public void Parse_IllFormedValue_ThrowsFormatException()
    {
        // Act / Assert
        Assert.Throws<FormatException>(() => ExerciseId.Parse("not-an-id"));
    }

    [Fact]
    public void Parse_NullValue_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => ExerciseId.Parse(null!));
    }

    [Fact]
    public void ToString_ParsedValue_RoundTripsOriginalValue()
    {
        // Arrange
        const string value = "EXR-7K2QXM";
        var id = ExerciseId.Parse(value);

        // Act
        var result = id.ToString();

        // Assert
        Assert.Equal(value, result);
    }
}
