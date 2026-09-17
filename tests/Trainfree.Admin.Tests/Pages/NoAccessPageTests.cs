using Bunit;
using Trainfree.Admin.Pages;

namespace Trainfree.Admin.Tests.Pages;

public sealed class NoAccessPageTests : BunitContext
{
    [Fact]
    public void Render_Always_ShowsNoAccessMessaging()
    {
        // Act
        var cut = Render<NoAccessPage>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid=no-access-page]"));
    }

    [Fact]
    public void Render_Always_ContainsNoLinkIntoAProtectedRoute()
    {
        // Act
        var cut = Render<NoAccessPage>();

        // Assert
        Assert.Empty(cut.FindAll("a"));
    }
}
