using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trainfree.Admin.Layout;

namespace Trainfree.Admin.Tests.Layout;

public sealed class NavMenuTests : BunitContext
{
    [Fact]
    public void Render_Always_ShowsExactlyHomePhasesAndProgramsLinks()
    {
        // Act
        var cut = Render<NavMenu>(p => p.Add(x => x.Collapsed, true));

        // Assert
        var links = cut.FindAll("a.nav-link");
        Assert.Equal(3, links.Count);
        Assert.Contains(
            links,
            l =>
                string.IsNullOrEmpty(l.GetAttribute("href"))
                && l.TextContent.Contains("Home", StringComparison.Ordinal)
        );
        Assert.Contains(
            links,
            l =>
                string.Equals(l.GetAttribute("href"), "phases", StringComparison.Ordinal)
                && l.TextContent.Contains("Phases", StringComparison.Ordinal)
        );
        Assert.Contains(
            links,
            l =>
                string.Equals(l.GetAttribute("href"), "programs", StringComparison.Ordinal)
                && l.TextContent.Contains("Programs", StringComparison.Ordinal)
        );
        Assert.DoesNotContain(
            links,
            l => l.TextContent.Contains("Admin", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Render_OnProgramsRoute_HighlightsProgramsLinkAndNotHome()
    {
        // Arrange
        Services.GetRequiredService<NavigationManager>().NavigateTo("/programs");

        // Act
        var cut = Render<NavMenu>(p => p.Add(x => x.Collapsed, false));

        // Assert
        var homeLink = cut.Find("a.nav-link[href='']");
        var phasesLink = cut.Find("a.nav-link[href='phases']");
        var programsLink = cut.Find("a.nav-link[href='programs']");
        Assert.Contains("active", programsLink.ClassList);
        Assert.DoesNotContain("active", homeLink.ClassList);
        Assert.DoesNotContain("active", phasesLink.ClassList);
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
