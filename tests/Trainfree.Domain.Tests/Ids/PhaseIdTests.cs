using Trainfree.Domain.Ids;

namespace Trainfree.Domain.Tests.Ids;

public sealed class PhaseIdTests
{
    [Theory]
    [InlineData("PHS-7K2QXM")]
    [InlineData("PHS-234567")]
    [InlineData("PHS-ABCDEF")]
    public void TryParse_WellFormedValue_ReturnsTrueAndParsedId(string value)
    {
        // Act
        var result = PhaseId.TryParse(value, out var id);

        // Assert
        Assert.True(result);
        Assert.Equal(value, id.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PHS-7K2QX")]
    [InlineData("PHS-7K2QXMM")]
    [InlineData("PHS-7K2Q0M")]
    [InlineData("PHS-7K2Q1M")]
    [InlineData("PHS-7K2QOM")]
    [InlineData("PHS-7K2QIM")]
    [InlineData("PHS-7K2QLM")]
    [InlineData("phs-7K2QXM")]
    [InlineData("XYZ-7K2QXM")]
    [InlineData("PHS7K2QXM")]
    public void TryParse_IllFormedValue_ReturnsFalse(string? value)
    {
        // Act
        var result = PhaseId.TryParse(value, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Parse_WellFormedValue_ReturnsParsedId()
    {
        // Act
        var id = PhaseId.Parse("PHS-7K2QXM");

        // Assert
        Assert.Equal("PHS-7K2QXM", id.ToString());
    }

    [Fact]
    public void Parse_IllFormedValue_ThrowsFormatException()
    {
        // Act / Assert
        Assert.Throws<FormatException>(() => PhaseId.Parse("not-an-id"));
    }

    [Fact]
    public void Parse_NullValue_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => PhaseId.Parse(null!));
    }

    [Fact]
    public void ToString_ParsedValue_RoundTripsOriginalValue()
    {
        // Arrange
        const string value = "PHS-7K2QXM";
        var id = PhaseId.Parse(value);

        // Act
        var result = id.ToString();

        // Assert
        Assert.Equal(value, result);
    }
}
