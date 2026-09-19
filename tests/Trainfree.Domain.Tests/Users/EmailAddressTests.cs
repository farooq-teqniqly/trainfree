using Trainfree.Domain.Users;

namespace Trainfree.Domain.Tests.Users;

public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("farooq@example.com", "F")]
    [InlineData("Farooq@example.com", "F")]
    [InlineData("z@example.com", "Z")]
    [InlineData("1abc@example.com", "1")]
    [InlineData("\U00010428bc@example.com", "\U00010400")]
    [InlineData("  farooq@example.com", "F")]
    public void Initial_Always_ReturnsTheUppercasedFirstCharacter(string value, string expected)
    {
        // Arrange
        var email = EmailAddress.Parse(value);

        // Act
        var initial = email.Initial;

        // Assert
        Assert.Equal(expected, initial);
    }

    [Fact]
    public void Parse_ValidValue_ReturnsAnAddressWhoseToStringIsTheOriginalValue()
    {
        // Act
        var email = EmailAddress.Parse("Farooq@Example.com");

        // Assert
        Assert.Equal("Farooq@Example.com", email.ToString());
    }

    [Fact]
    public void Parse_Null_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => EmailAddress.Parse(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_ThrowsArgumentException(string value)
    {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => EmailAddress.Parse(value));
    }

    [Fact]
    public void Initial_DefaultInstance_ReturnsEmptyString()
    {
        // Arrange
        var email = default(EmailAddress);

        // Act
        var initial = email.Initial;

        // Assert
        Assert.Equal(string.Empty, initial);
    }

    [Fact]
    public void Parse_SurroundingWhitespace_ReturnsTheTrimmedAddress()
    {
        // Act
        var email = EmailAddress.Parse("  farooq@example.com 	");

        // Assert
        Assert.Equal("farooq@example.com", email.ToString());
    }
}
