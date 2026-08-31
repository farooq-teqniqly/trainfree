using Trainfree.Domain.Ids;

namespace Trainfree.Domain.Tests.Ids;

public sealed class CategoryIdTests
{
    [Theory]
    [InlineData("CAT-7K2QXM")]
    [InlineData("CAT-234567")]
    [InlineData("CAT-ABCDEF")]
    public void TryParse_WellFormedValue_ReturnsTrueAndParsedId(string value)
    {
        // Act
        var result = CategoryId.TryParse(value, out var id);

        // Assert
        Assert.True(result);
        Assert.Equal(value, id.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("CAT-7K2QX")]
    [InlineData("CAT-7K2QXMM")]
    [InlineData("CAT-7K2Q0M")]
    [InlineData("CAT-7K2Q1M")]
    [InlineData("CAT-7K2QOM")]
    [InlineData("CAT-7K2QIM")]
    [InlineData("CAT-7K2QLM")]
    [InlineData("cat-7K2QXM")]
    [InlineData("XYZ-7K2QXM")]
    [InlineData("CAT7K2QXM")]
    public void TryParse_IllFormedValue_ReturnsFalse(string? value)
    {
        // Act
        var result = CategoryId.TryParse(value, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Parse_WellFormedValue_ReturnsParsedId()
    {
        // Act
        var id = CategoryId.Parse("CAT-7K2QXM");

        // Assert
        Assert.Equal("CAT-7K2QXM", id.ToString());
    }

    [Fact]
    public void Parse_IllFormedValue_ThrowsFormatException()
    {
        // Act / Assert
        Assert.Throws<FormatException>(() => CategoryId.Parse("not-an-id"));
    }

    [Fact]
    public void ToString_ParsedValue_RoundTripsOriginalValue()
    {
        // Arrange
        const string value = "CAT-7K2QXM";
        var id = CategoryId.Parse(value);

        // Act
        var result = id.ToString();

        // Assert
        Assert.Equal(value, result);
    }
}
