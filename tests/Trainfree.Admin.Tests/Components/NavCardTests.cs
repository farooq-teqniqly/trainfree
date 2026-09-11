using Bunit;
using Trainfree.Admin.Components;

namespace Trainfree.Admin.Tests.Components;

public sealed class NavCardTests : BunitContext
{
    [Fact]
    public void Render_Always_RendersLinkWithHrefAndTestId()
    {
        // Arrange / Act
        var cut = Render<NavCard>(p =>
            p.Add(c => c.Href, "programs").Add(c => c.TestId, "home-tile-programs")
        );

        // Assert
        var tile = cut.Find("[data-testid='home-tile-programs']");
        Assert.Equal("a", tile.TagName, ignoreCase: true);
        Assert.Equal("programs", tile.GetAttribute("href"));
    }

    [Fact]
    public void Render_Always_RendersTitleDescriptionAndLinkLabel()
    {
        // Arrange / Act
        var cut = Render<NavCard>(p =>
            p.Add(c => c.Href, "programs")
                .Add(c => c.TestId, "home-tile-programs")
                .Add(c => c.Title, "Programs")
                .Add(c => c.Description, "Build out programs.")
                .Add(c => c.LinkLabel, "Manage Programs")
        );

        // Assert
        Assert.Equal("Programs", cut.Find(".card-title").TextContent.Trim());
        Assert.Equal("Build out programs.", cut.Find(".card-text").TextContent.Trim());
        Assert.Equal("Manage Programs", cut.Find(".tile-link").TextContent.Trim());
    }

    [Fact]
    public void Render_IconClassIsSet_AppliesIconClass()
    {
        // Arrange / Act
        var cut = Render<NavCard>(p =>
            p.Add(c => c.Href, "programs")
                .Add(c => c.TestId, "home-tile-programs")
                .Add(c => c.IconClass, "bi-grid-3x3-gap-fill")
        );

        // Assert
        var icon = cut.Find(".tile-icon i");
        Assert.Contains("bi-grid-3x3-gap-fill", icon.ClassList);
    }
}
