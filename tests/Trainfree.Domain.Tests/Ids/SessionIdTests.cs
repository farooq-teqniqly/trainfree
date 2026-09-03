using Trainfree.Domain.Ids;

namespace Trainfree.Domain.Tests.Ids;

public sealed class SessionIdTests
{
    [Theory]
    [InlineData("SNN-7K2QXM")]
    [InlineData("SNN-234567")]
    [InlineData("SNN-ABCDEF")]
    public void TryParse_WellFormedValue_ReturnsTrueAndParsedId(string value)
    {
        // Act
        var result = SessionId.TryParse(value, out var id);

        // Assert
        Assert.True(result);
        Assert.Equal(value, id.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SNN-7K2QX")]
    [InlineData("SNN-7K2QXMM")]
    [InlineData("SNN-7K2Q0M")]
    [InlineData("SNN-7K2Q1M")]
    [InlineData("SNN-7K2QOM")]
    [InlineData("SNN-7K2QIM")]
    [InlineData("SNN-7K2QLM")]
    [InlineData("snn-7K2QXM")]
    [InlineData("XYZ-7K2QXM")]
    [InlineData("SNN7K2QXM")]
    public void TryParse_IllFormedValue_ReturnsFalse(string? value)
    {
        // Act
        var result = SessionId.TryParse(value, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Parse_WellFormedValue_ReturnsParsedId()
    {
        // Act
        var id = SessionId.Parse("SNN-7K2QXM");

        // Assert
        Assert.Equal("SNN-7K2QXM", id.ToString());
    }

    [Fact]
    public void Parse_IllFormedValue_ThrowsFormatException()
    {
        // Act / Assert
        Assert.Throws<FormatException>(() => SessionId.Parse("not-an-id"));
    }

    [Fact]
    public void Parse_NullValue_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => SessionId.Parse(null!));
    }

    [Fact]
    public void ToString_ParsedValue_RoundTripsOriginalValue()
    {
        // Arrange
        const string value = "SNN-7K2QXM";
        var id = SessionId.Parse(value);

        // Act
        var result = id.ToString();

        // Assert
        Assert.Equal(value, result);
    }
}
