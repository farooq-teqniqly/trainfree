using Trainfree.Domain.Ids;

namespace Trainfree.Domain.Tests.Ids;

public sealed class SessionPhaseIdTests
{
    [Theory]
    [InlineData("SPH-7K2QXM")]
    [InlineData("SPH-234567")]
    [InlineData("SPH-ABCDEF")]
    public void TryParse_WellFormedValue_ReturnsTrueAndParsedId(string value)
    {
        // Act
        var result = SessionPhaseId.TryParse(value, out var id);

        // Assert
        Assert.True(result);
        Assert.Equal(value, id.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SPH-7K2QX")]
    [InlineData("SPH-7K2QXMM")]
    [InlineData("SPH-7K2Q0M")]
    [InlineData("SPH-7K2Q1M")]
    [InlineData("SPH-7K2QOM")]
    [InlineData("SPH-7K2QIM")]
    [InlineData("SPH-7K2QLM")]
    [InlineData("sph-7K2QXM")]
    [InlineData("XYZ-7K2QXM")]
    [InlineData("SPH7K2QXM")]
    public void TryParse_IllFormedValue_ReturnsFalse(string? value)
    {
        // Act
        var result = SessionPhaseId.TryParse(value, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Parse_WellFormedValue_ReturnsParsedId()
    {
        // Act
        var id = SessionPhaseId.Parse("SPH-7K2QXM");

        // Assert
        Assert.Equal("SPH-7K2QXM", id.ToString());
    }

    [Fact]
    public void Parse_IllFormedValue_ThrowsFormatException()
    {
        // Act / Assert
        Assert.Throws<FormatException>(() => SessionPhaseId.Parse("not-an-id"));
    }

    [Fact]
    public void Parse_NullValue_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => SessionPhaseId.Parse(null!));
    }

    [Fact]
    public void ToString_ParsedValue_RoundTripsOriginalValue()
    {
        // Arrange
        const string value = "SPH-7K2QXM";
        var id = SessionPhaseId.Parse(value);

        // Act
        var result = id.ToString();

        // Assert
        Assert.Equal(value, result);
    }
}
