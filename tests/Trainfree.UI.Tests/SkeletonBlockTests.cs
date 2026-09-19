using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trainfree.UI.Tests;

public sealed class SkeletonBlockTests : BunitContext
{
    [Fact]
    public void Render_WidthSpecified_RendersPlaceholderElementWithInlineWidth()
    {
        // Arrange / Act
        var cut = Render<SkeletonBlock>(parameters => parameters.Add(p => p.Width, "60%"));

        // Assert
        var element = cut.Find(".placeholder");
        Assert.Equal("width: 60%;", element.GetAttribute("style"));
    }

    [Fact]
    public void Render_TwoInstancesInsideSharedShimmerWrapper_NeitherAddsItsOwnPlaceholderWaveClass()
    {
        // Arrange
        RenderFragment fragment = BuildTwoSkeletonBlocksInShimmerWrapper;

        // Act
        var cut = Render(fragment);

        // Assert
        var blocks = cut.FindAll(".placeholder");
        Assert.Equal(2, blocks.Count);
        Assert.All(blocks, b => Assert.False(b.ClassList.Contains("placeholder-wave")));
    }

    private static void BuildTwoSkeletonBlocksInShimmerWrapper(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "placeholder-wave");
        builder.OpenComponent<SkeletonBlock>(2);
        builder.AddAttribute(3, "Width", "50%");
        builder.CloseComponent();
        builder.OpenComponent<SkeletonBlock>(4);
        builder.AddAttribute(5, "Width", "30%");
        builder.CloseComponent();
        builder.CloseElement();
    }
}
