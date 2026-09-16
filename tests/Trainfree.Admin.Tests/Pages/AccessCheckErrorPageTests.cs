using Bunit;
using Trainfree.Admin.Pages;

namespace Trainfree.Admin.Tests.Pages;

public sealed class AccessCheckErrorPageTests : BunitContext
{
    [Fact]
    public void Render_Always_ShowsRetryMessagingDistinctFromNoAccessPage()
    {
        // Arrange
        var noAccess = Render<NoAccessPage>();

        // Act
        var cut = Render<AccessCheckErrorPage>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid=access-check-error-page]"));
        Assert.NotEqual(noAccess.Markup, cut.Markup);
    }
}
