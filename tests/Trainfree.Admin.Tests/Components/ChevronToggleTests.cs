using Bunit;
using Trainfree.Admin.Components;

namespace Trainfree.Admin.Tests.Components;

public sealed class ChevronToggleTests : BunitContext
{
    [Fact]
    public void Render_HasContentIsFalse_ButtonIsDisabled()
    {
        // Arrange / Act
        var cut = Render<ChevronToggle>(p =>
            p.Add(c => c.TestId, "chevron").Add(c => c.HasContent, false)
        );

        // Assert
        var button = cut.Find("[data-testid='chevron']");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_HasContentIsFalseAndCollapsed_ChevronIsNotCollapsedClass()
    {
        // Arrange / Act
        var cut = Render<ChevronToggle>(p =>
            p.Add(c => c.TestId, "chevron")
                .Add(c => c.HasContent, false)
                .Add(c => c.IsCollapsed, true)
        );

        // Assert
        var icon = cut.Find("i");
        Assert.DoesNotContain("chevron-collapsed", icon.ClassList);
    }

    [Fact]
    public void Render_HasContentIsTrueAndNotCollapsed_ButtonIsExpanded()
    {
        // Arrange / Act
        var cut = Render<ChevronToggle>(p =>
            p.Add(c => c.TestId, "chevron")
                .Add(c => c.HasContent, true)
                .Add(c => c.IsCollapsed, false)
        );

        // Assert
        var button = cut.Find("[data-testid='chevron']");
        Assert.False(button.HasAttribute("disabled"));
        Assert.Equal("true", button.GetAttribute("aria-expanded"));
        var icon = cut.Find("i");
        Assert.DoesNotContain("chevron-collapsed", icon.ClassList);
    }

    [Fact]
    public void Render_HasContentIsTrueAndCollapsed_ButtonIsCollapsed()
    {
        // Arrange / Act
        var cut = Render<ChevronToggle>(p =>
            p.Add(c => c.TestId, "chevron")
                .Add(c => c.HasContent, true)
                .Add(c => c.IsCollapsed, true)
        );

        // Assert
        var button = cut.Find("[data-testid='chevron']");
        Assert.Equal("false", button.GetAttribute("aria-expanded"));
        var icon = cut.Find("i");
        Assert.Contains("chevron-collapsed", icon.ClassList);
    }

    [Fact]
    public void Click_HasContentIsTrue_InvokesOnToggle()
    {
        // Arrange
        var toggled = false;
        var cut = Render<ChevronToggle>(p =>
            p.Add(c => c.TestId, "chevron")
                .Add(c => c.HasContent, true)
                .Add(c => c.OnToggle, () => toggled = true)
        );

        // Act
        cut.Find("[data-testid='chevron']").Click();

        // Assert
        Assert.True(toggled);
    }
}
