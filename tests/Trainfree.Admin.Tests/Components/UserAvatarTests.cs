using Bunit;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Components;
using Trainfree.Domain.Users;

namespace Trainfree.Admin.Tests.Components;

public sealed class UserAvatarTests : BunitContext
{
    private static readonly CurrentUser Farooq = new(
        EmailAddress.Parse("farooq@example.com"),
        "Administrator"
    );

    [Fact]
    public void Render_Always_ShowsTheUppercasedFirstLetterOfTheEmailInTheAvatar()
    {
        // Arrange / Act
        var cut = Render<UserAvatar>(p => p.Add(x => x.User, Farooq));

        // Assert
        Assert.Equal("F", cut.Find("[data-testid=user-avatar]").TextContent.Trim());
    }

    [Fact]
    public void Render_Always_ExposesTheFullEmailAsTheTooltip()
    {
        // Arrange / Act
        var cut = Render<UserAvatar>(p => p.Add(x => x.User, Farooq));

        // Assert
        Assert.Equal(
            "farooq@example.com",
            cut.Find("[data-testid=user-avatar]").GetAttribute("title")
        );
    }

    [Fact]
    public void Render_Always_ExposesTheFullEmailToAssistiveTech()
    {
        // Arrange / Act
        var cut = Render<UserAvatar>(p => p.Add(x => x.User, Farooq));

        // Assert
        var avatar = cut.Find("[data-testid=user-avatar]");
        Assert.Equal("img", avatar.GetAttribute("role"));
        Assert.Equal("Signed in as farooq@example.com", avatar.GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_Always_ShowsTheRoleBeneathTheAvatar()
    {
        // Arrange / Act
        var cut = Render<UserAvatar>(p => p.Add(x => x.User, Farooq));

        // Assert
        var children = cut.Find("[data-testid=user-identity]").Children;
        Assert.Equal("user-avatar", children[0].GetAttribute("data-testid"));
        Assert.Equal("user-role", children[1].GetAttribute("data-testid"));
        Assert.Equal("Administrator", children[1].TextContent.Trim());
    }
}
