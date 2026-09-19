using Trainfree.Domain.Users;

namespace Trainfree.Domain.Tests.Users;

public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("farooq@example.com", "F")]
    [InlineData("Farooq@example.com", "F")]
    [InlineData("z@example.com", "Z")]
    [InlineData("1abc@example.com", "1")]
    [InlineData("\U0001D4D0bc@example.com", "\U0001D4D0")]
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
    public void Equals_SameValue_AreEqual()
    {
        // Arrange
        var first = EmailAddress.Parse("a@example.com");
        var second = EmailAddress.Parse("a@example.com");

        // Act / Assert
        Assert.Equal(first, second);
    }
}
