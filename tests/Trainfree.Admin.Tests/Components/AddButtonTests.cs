using Bunit;
using Trainfree.Admin.Components;

namespace Trainfree.Admin.Tests.Components;

public sealed class AddButtonTests : BunitContext
{
    [Fact]
    public void Render_Always_RendersButtonWithTestIdAndLabel()
    {
        // Arrange / Act
        var cut = Render<AddButton>(p =>
            p.Add(c => c.TestId, "add-program").Add(c => c.Label, "Add Program")
        );

        // Assert
        var button = cut.Find("[data-testid='add-program']");
        Assert.Equal("Add Program", button.TextContent.Trim());
    }

    [Fact]
    public void Click_Always_InvokesOnClick()
    {
        // Arrange
        var clicked = false;
        var cut = Render<AddButton>(p =>
            p.Add(c => c.TestId, "add-program").Add(c => c.OnClick, () => clicked = true)
        );

        // Act
        cut.Find("[data-testid='add-program']").Click();

        // Assert
        Assert.True(clicked);
    }
}
