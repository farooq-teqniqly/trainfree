using Bunit;
using Trainfree.Web.Layout;

namespace Trainfree.Web.Tests.Layout;

public sealed class NavMenuTests : BunitContext
{
    [Fact]
    public void Render_Always_ShowsHomeAndAdminLinks()
    {
        // Act
        var cut = Render<NavMenu>(p => p.Add(x => x.Collapsed, true));

        // Assert
        var links = cut.FindAll("a.nav-link");
        Assert.Contains(
            links,
            l =>
                string.IsNullOrEmpty(l.GetAttribute("href"))
                && l.TextContent.Contains("Home", StringComparison.Ordinal)
        );
        Assert.Contains(
            links,
            l =>
                string.Equals(l.GetAttribute("href"), "admin", StringComparison.Ordinal)
                && l.TextContent.Contains("Admin", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Render_CollapsedIsTrue_AppliesTheCollapseClass()
    {
        // Act
        var cut = Render<NavMenu>(p => p.Add(x => x.Collapsed, true));

        // Assert
        Assert.Contains("collapse", cut.Find("nav.sidebar").ClassList);
    }

    [Fact]
    public void Render_CollapsedIsFalse_DoesNotApplyTheCollapseClass()
    {
        // Act
        var cut = Render<NavMenu>(p => p.Add(x => x.Collapsed, false));

        // Assert
        Assert.DoesNotContain("collapse", cut.Find("nav.sidebar").ClassList);
    }
}
