using Bunit;
using Trainfree.Admin.Pages;

namespace Trainfree.Admin.Tests.Pages;

public sealed class HomeTests : BunitContext
{
    [Fact]
    public void Render_Always_ProgramsTileLinksToProgramsRoute()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var tile = cut.Find("[data-testid='home-tile-programs']");
        Assert.Equal("a", tile.TagName, ignoreCase: true);
        Assert.Equal("programs", tile.GetAttribute("href"));
    }

    [Fact]
    public void Render_Always_PhasesTileLinksToPhasesRoute()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var tile = cut.Find("[data-testid='home-tile-phases']");
        Assert.Equal("a", tile.TagName, ignoreCase: true);
        Assert.Equal("phases", tile.GetAttribute("href"));
    }

    [Fact]
    public void Render_Always_ExercisesTileRendersWithoutALink()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var tile = cut.Find("[data-testid='home-tile-exercises']");
        Assert.NotEqual("a", tile.TagName, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Exercises", tile.TextContent, StringComparison.Ordinal);
    }
}
