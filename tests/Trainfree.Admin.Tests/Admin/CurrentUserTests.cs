using Trainfree.Admin.Admin;
using Trainfree.Domain.Users;

namespace Trainfree.Admin.Tests.Admin;

public sealed class CurrentUserTests
{
    [Fact]
    public void Constructor_ValidArguments_ExposesEmailAndRole()
    {
        // Arrange / Act
        var user = new CurrentUser(EmailAddress.Parse("a@x.com"), "Administrator");

        // Assert
        Assert.Equal("a@x.com", user.Email.ToString());
        Assert.Equal("Administrator", user.Role);
    }

    [Fact]
    public void Constructor_NullRole_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CurrentUser(EmailAddress.Parse("a@x.com"), null!)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankRole_ThrowsArgumentException(string role)
    {
        // Act / Assert
        Assert.Throws<ArgumentException>(() =>
            new CurrentUser(EmailAddress.Parse("a@x.com"), role)
        );
    }
}
