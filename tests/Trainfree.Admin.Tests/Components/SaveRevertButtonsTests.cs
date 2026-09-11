using Bunit;
using Trainfree.Admin.Components;

namespace Trainfree.Admin.Tests.Components;

public sealed class SaveRevertButtonsTests : BunitContext
{
    [Fact]
    public void Render_IsDirtyIsFalse_RendersNothing()
    {
        // Arrange / Act
        var cut = Render<SaveRevertButtons>(p =>
            p.Add(c => c.SaveTestId, "save")
                .Add(c => c.RevertTestId, "revert")
                .Add(c => c.IsDirty, false)
        );

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void Render_IsDirtyIsTrue_RendersSaveAndRevertButtons()
    {
        // Arrange / Act
        var cut = Render<SaveRevertButtons>(p =>
            p.Add(c => c.SaveTestId, "save")
                .Add(c => c.RevertTestId, "revert")
                .Add(c => c.IsDirty, true)
        );

        // Assert
        Assert.NotNull(cut.Find("[data-testid='save']"));
        Assert.NotNull(cut.Find("[data-testid='revert']"));
    }

    [Fact]
    public void Render_IsSavingIsTrue_DisablesBothButtons()
    {
        // Arrange / Act
        var cut = Render<SaveRevertButtons>(p =>
            p.Add(c => c.SaveTestId, "save")
                .Add(c => c.RevertTestId, "revert")
                .Add(c => c.IsDirty, true)
                .Add(c => c.IsSaving, true)
        );

        // Assert
        Assert.True(cut.Find("[data-testid='save']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='revert']").HasAttribute("disabled"));
    }

    [Fact]
    public void Click_SaveButton_InvokesOnSave()
    {
        // Arrange
        var saved = false;
        var cut = Render<SaveRevertButtons>(p =>
            p.Add(c => c.SaveTestId, "save")
                .Add(c => c.RevertTestId, "revert")
                .Add(c => c.IsDirty, true)
                .Add(c => c.OnSave, () => saved = true)
        );

        // Act
        cut.Find("[data-testid='save']").Click();

        // Assert
        Assert.True(saved);
    }

    [Fact]
    public void Click_RevertButton_InvokesOnRevert()
    {
        // Arrange
        var reverted = false;
        var cut = Render<SaveRevertButtons>(p =>
            p.Add(c => c.SaveTestId, "save")
                .Add(c => c.RevertTestId, "revert")
                .Add(c => c.IsDirty, true)
                .Add(c => c.OnRevert, () => reverted = true)
        );

        // Act
        cut.Find("[data-testid='revert']").Click();

        // Assert
        Assert.True(reverted);
    }
}
