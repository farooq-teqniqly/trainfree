using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Trainfree.Admin.Components;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Components;

public sealed class ProgramTableRowTests : BunitContext
{
    private static ProgramRow CreateRow() => new(ProgramId.Parse("PRG-AAAAAA"), "Workout A");

    private IRenderedComponent<ProgramTableRow> RenderRow(
        ProgramRow row,
        bool isSaving = false,
        Action? onToggle = null,
        Action? onSave = null,
        Action? onRevert = null,
        Action? onAddSession = null,
        Action? onDelete = null
    )
    {
        row.IsSaving = isSaving;
        return Render<ProgramTableRow>(p =>
            p.Add(c => c.Row, row)
                .Add(c => c.IsCollapsed, false)
                .Add(c => c.IsEditing, false)
                .Add(c => c.ChevronTitle, "Toggle")
                .Add(c => c.HasToggleableContent, true)
                .Add(c => c.OnToggle, onToggle ?? (() => { }))
                .Add(c => c.OnSave, onSave ?? (() => { }))
                .Add(c => c.OnRevert, onRevert ?? (() => { }))
                .Add(c => c.OnAddSession, onAddSession ?? (() => { }))
                .Add(c => c.OnDelete, onDelete ?? (() => { }))
        );
    }

    [Fact]
    public void Render_Row_ShowsNameValue()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = RenderRow(row);

        // Assert
        Assert.Equal(
            "Workout A",
            cut.Find("[data-testid='name-input-PRG-AAAAAA']").GetAttribute("value")
        );
    }

    [Fact]
    public void Render_RowIsClean_HidesSaveAndRevertButtons()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = RenderRow(row);

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='save-PRG-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='revert-PRG-AAAAAA']"));
    }

    [Fact]
    public void Input_NameFieldEdited_ShowsSaveAndRevertButtons()
    {
        // Arrange
        var row = CreateRow();
        var cut = RenderRow(row);

        // Act
        cut.Find("[data-testid='name-input-PRG-AAAAAA']").Input("Workout B");

        // Assert
        Assert.Single(cut.FindAll("[data-testid='save-PRG-AAAAAA']"));
        Assert.Single(cut.FindAll("[data-testid='revert-PRG-AAAAAA']"));
    }

    [Fact]
    public void Click_SaveButton_InvokesOnSave()
    {
        // Arrange
        var row = CreateRow();
        var saved = false;
        var cut = RenderRow(row, onSave: () => saved = true);
        cut.Find("[data-testid='name-input-PRG-AAAAAA']").Input("Workout B");

        // Act
        cut.Find("[data-testid='save-PRG-AAAAAA']").Click();

        // Assert
        Assert.True(saved);
    }

    [Fact]
    public void Click_RevertButton_InvokesOnRevert()
    {
        // Arrange
        var row = CreateRow();
        var reverted = false;
        var cut = RenderRow(row, onRevert: () => reverted = true);
        cut.Find("[data-testid='name-input-PRG-AAAAAA']").Input("Workout B");

        // Act
        cut.Find("[data-testid='revert-PRG-AAAAAA']").Click();

        // Assert
        Assert.True(reverted);
    }

    [Fact]
    public void KeyDown_EnterOnDirtyField_InvokesOnSave()
    {
        // Arrange
        var row = CreateRow();
        var saved = false;
        var cut = RenderRow(row, onSave: () => saved = true);
        var input = cut.Find("[data-testid='name-input-PRG-AAAAAA']");
        input.Input("Workout B");

        // Act
        input.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        Assert.True(saved);
    }

    [Fact]
    public void KeyDown_EnterOnCleanField_DoesNotInvokeOnSave()
    {
        // Arrange
        var row = CreateRow();
        var saved = false;
        var cut = RenderRow(row, onSave: () => saved = true);

        // Act
        cut.Find("[data-testid='name-input-PRG-AAAAAA']")
            .KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        Assert.False(saved);
    }

    [Fact]
    public void Click_AddSessionButton_InvokesOnAddSession()
    {
        // Arrange
        var row = CreateRow();
        var added = false;
        var cut = RenderRow(row, onAddSession: () => added = true);

        // Act
        cut.Find("[data-testid='add-session-PRG-AAAAAA']").Click();

        // Assert
        Assert.True(added);
    }

    [Fact]
    public void Click_DeleteButton_InvokesOnDelete()
    {
        // Arrange
        var row = CreateRow();
        var deleted = false;
        var cut = RenderRow(row, onDelete: () => deleted = true);

        // Act
        cut.Find("[data-testid='delete-PRG-AAAAAA']").Click();

        // Assert
        Assert.True(deleted);
    }

    [Fact]
    public void Click_ChevronButton_InvokesOnToggle()
    {
        // Arrange
        var row = CreateRow();
        var toggled = false;
        var cut = RenderRow(row, onToggle: () => toggled = true);

        // Act
        cut.Find("[data-testid='chevron-PRG-AAAAAA']").Click();

        // Assert
        Assert.True(toggled);
    }

    [Fact]
    public void Render_RowIsSaving_DisablesNameInput()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = RenderRow(row, isSaving: true);

        // Assert
        Assert.True(cut.Find("[data-testid='name-input-PRG-AAAAAA']").HasAttribute("disabled"));
    }
}
