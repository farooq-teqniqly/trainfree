using Bunit;
using Trainfree.Admin.Components;

namespace Trainfree.Admin.Tests.Components;

public sealed class DeleteButtonTests : BunitContext
{
    [Fact]
    public void Render_Always_RendersButtonWithTestId()
    {
        // Arrange / Act
        var cut = Render<DeleteButton>(p => p.Add(c => c.TestId, "delete-1"));

        // Assert
        var button = cut.Find("[data-testid='delete-1']");
        Assert.False(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_DisabledIsTrue_ButtonIsDisabled()
    {
        // Arrange / Act
        var cut = Render<DeleteButton>(p =>
            p.Add(c => c.TestId, "delete-1").Add(c => c.Disabled, true)
        );

        // Assert
        var button = cut.Find("[data-testid='delete-1']");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void Click_Always_InvokesOnClick()
    {
        // Arrange
        var clicked = false;
        var cut = Render<DeleteButton>(p =>
            p.Add(c => c.TestId, "delete-1").Add(c => c.OnClick, () => clicked = true)
        );

        // Act
        cut.Find("[data-testid='delete-1']").Click();

        // Assert
        Assert.True(clicked);
    }
}
