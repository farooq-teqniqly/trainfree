using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trainfree.Admin.Layout;

namespace Trainfree.Admin.Tests.Layout;

public sealed class NavMenuTests : BunitContext
{
    [Fact]
    public void Render_Always_ShowsExactlyHomePhasesExercisesAndProgramsLinksInOrder()
    {
        // Act
        var cut = Render<NavMenu>(p => p.Add(x => x.Collapsed, true));

        // Assert
        var links = cut.FindAll("a.nav-link");
        Assert.Equal(4, links.Count);
        Assert.True(string.IsNullOrEmpty(links[0].GetAttribute("href")));
        Assert.Contains("Home", links[0].TextContent, StringComparison.Ordinal);
        Assert.Equal("phases", links[1].GetAttribute("href"));
        Assert.Contains("Phases", links[1].TextContent, StringComparison.Ordinal);
        Assert.Equal("exercises", links[2].GetAttribute("href"));
        Assert.Contains("Exercises", links[2].TextContent, StringComparison.Ordinal);
        Assert.Equal("programs", links[3].GetAttribute("href"));
        Assert.Contains("Programs", links[3].TextContent, StringComparison.Ordinal);
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
        var exercisesLink = cut.Find("a.nav-link[href='exercises']");
        var programsLink = cut.Find("a.nav-link[href='programs']");
        Assert.Contains("active", programsLink.ClassList);
        Assert.DoesNotContain("active", homeLink.ClassList);
        Assert.DoesNotContain("active", phasesLink.ClassList);
        Assert.DoesNotContain("active", exercisesLink.ClassList);
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
