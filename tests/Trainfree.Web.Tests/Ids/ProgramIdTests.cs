using Trainfree.Web.Ids;

namespace Trainfree.Web.Tests.Ids;

public sealed class ProgramIdTests
{
    [Theory]
    [InlineData("PRG-7K2QXM")]
    [InlineData("PRG-234567")]
    [InlineData("PRG-ABCDEF")]
    public void TryParse_WellFormedValue_ReturnsTrueAndParsedId(string value)
    {
        // Act
        var result = ProgramId.TryParse(value, out var id);

        // Assert
        Assert.True(result);
        Assert.Equal(value, id.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PRG-7K2QX")]
    [InlineData("PRG-7K2QXMM")]
    [InlineData("PRG-7K2Q0M")]
    [InlineData("PRG-7K2Q1M")]
    [InlineData("PRG-7K2QOM")]
    [InlineData("PRG-7K2QIM")]
    [InlineData("PRG-7K2QLM")]
    [InlineData("prg-7K2QXM")]
    [InlineData("XYZ-7K2QXM")]
    [InlineData("PRG7K2QXM")]
    public void TryParse_IllFormedValue_ReturnsFalse(string? value)
    {
        // Act
        var result = ProgramId.TryParse(value!, out _);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Parse_WellFormedValue_ReturnsParsedId()
    {
        // Act
        var id = ProgramId.Parse("PRG-7K2QXM");

        // Assert
        Assert.Equal("PRG-7K2QXM", id.ToString());
    }

    [Fact]
    public void Parse_IllFormedValue_ThrowsFormatException()
    {
        // Act / Assert
        Assert.Throws<FormatException>(() => ProgramId.Parse("not-an-id"));
    }

    [Fact]
    public void ToString_ParsedValue_RoundTripsOriginalValue()
    {
        // Arrange
        const string value = "PRG-7K2QXM";
        var id = ProgramId.Parse(value);

        // Act
        var result = id.ToString();

        // Assert
        Assert.Equal(value, result);
    }
}
