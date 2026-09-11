using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Components;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Components;

public sealed class SessionTableRowTests : BunitContext
{
    private static SessionRow CreateRow() =>
        new(SessionId.Parse("SNN-AAAAAA"), ProgramId.Parse("PRG-AAAAAA"), "Session A");

    private IRenderedComponent<SessionTableRow> RenderRow(
        SessionRow row,
        bool isSaving = false,
        bool isAddingPhase = false,
        IReadOnlyList<PhaseSummary>? phaseLibrary = null,
        string? phaseLibraryLoadError = null,
        Action? onToggle = null,
        Action? onSave = null,
        Action? onRevert = null,
        Action? onAddPhase = null,
        Action? onDelete = null,
        Action<string>? onPhaseSelected = null
    )
    {
        row.IsSaving = isSaving;
        return Render<SessionTableRow>(p =>
            p.Add(c => c.Session, row)
                .Add(c => c.IsCollapsed, false)
                .Add(c => c.IsEditing, false)
                .Add(c => c.ChevronTitle, "Toggle")
                .Add(c => c.HasToggleableContent, true)
                .Add(c => c.IsAddingPhase, isAddingPhase)
                .Add(c => c.PhaseLibrary, phaseLibrary ?? [])
                .Add(c => c.PhaseLibraryLoadError, phaseLibraryLoadError)
                .Add(c => c.OnToggle, onToggle ?? (() => { }))
                .Add(c => c.OnSave, onSave ?? (() => { }))
                .Add(c => c.OnRevert, onRevert ?? (() => { }))
                .Add(c => c.OnAddPhase, onAddPhase ?? (() => { }))
                .Add(c => c.OnDelete, onDelete ?? (() => { }))
                .Add(c => c.OnPhaseSelected, onPhaseSelected ?? (_ => { }))
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
            "Session A",
            cut.Find("[data-testid='session-name-input-SNN-AAAAAA']").GetAttribute("value")
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
        Assert.Empty(cut.FindAll("[data-testid='session-save-SNN-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='session-revert-SNN-AAAAAA']"));
    }

    [Fact]
    public void Input_NameFieldEdited_ShowsSaveAndRevertButtons()
    {
        // Arrange
        var row = CreateRow();
        var cut = RenderRow(row);

        // Act
        cut.Find("[data-testid='session-name-input-SNN-AAAAAA']").Input("Session B");

        // Assert
        Assert.Single(cut.FindAll("[data-testid='session-save-SNN-AAAAAA']"));
        Assert.Single(cut.FindAll("[data-testid='session-revert-SNN-AAAAAA']"));
    }

    [Fact]
    public void Click_SaveButton_InvokesOnSave()
    {
        // Arrange
        var row = CreateRow();
        var saved = false;
        var cut = RenderRow(row, onSave: () => saved = true);
        cut.Find("[data-testid='session-name-input-SNN-AAAAAA']").Input("Session B");

        // Act
        cut.Find("[data-testid='session-save-SNN-AAAAAA']").Click();

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
        cut.Find("[data-testid='session-name-input-SNN-AAAAAA']").Input("Session B");

        // Act
        cut.Find("[data-testid='session-revert-SNN-AAAAAA']").Click();

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
        var input = cut.Find("[data-testid='session-name-input-SNN-AAAAAA']");
        input.Input("Session B");

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
        cut.Find("[data-testid='session-name-input-SNN-AAAAAA']")
            .KeyDown(new KeyboardEventArgs { Key = "Enter" });

        // Assert
        Assert.False(saved);
    }

    [Fact]
    public void Click_AddPhaseButton_InvokesOnAddPhase()
    {
        // Arrange
        var row = CreateRow();
        var added = false;
        var cut = RenderRow(row, onAddPhase: () => added = true);

        // Act
        cut.Find("[data-testid='add-phase-SNN-AAAAAA']").Click();

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
        cut.Find("[data-testid='session-delete-SNN-AAAAAA']").Click();

        // Assert
        Assert.True(deleted);
    }

    [Fact]
    public void Render_IsAddingPhaseWithEmptyLibrary_ShowsEmptyLibraryMessage()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = RenderRow(row, isAddingPhase: true);

        // Assert
        Assert.Single(cut.FindAll("[data-testid='empty-phase-library-SNN-AAAAAA']"));
    }

    [Fact]
    public void Render_IsAddingPhaseWithLibrary_ShowsPhasePicker()
    {
        // Arrange
        var row = CreateRow();
        var library = new[] { new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up") };

        // Act
        var cut = RenderRow(row, isAddingPhase: true, phaseLibrary: library);

        // Assert
        Assert.Single(cut.FindAll("[data-testid='phase-picker-SNN-AAAAAA']"));
    }

    [Fact]
    public void Change_PhasePicker_InvokesOnPhaseSelected()
    {
        // Arrange
        var row = CreateRow();
        var library = new[] { new PhaseSummary(PhaseId.Parse("PHS-AAAAAA"), "Warm Up") };
        string? selected = null;
        var cut = RenderRow(
            row,
            isAddingPhase: true,
            phaseLibrary: library,
            onPhaseSelected: value => selected = value
        );

        // Act
        cut.Find("[data-testid='phase-picker-SNN-AAAAAA']").Change("PHS-AAAAAA");

        // Assert
        Assert.Equal("PHS-AAAAAA", selected);
    }

    [Fact]
    public void Render_IsAddingPhaseWithLoadError_ShowsLoadError()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = RenderRow(
            row,
            isAddingPhase: true,
            phaseLibraryLoadError: "Failed to load phases."
        );

        // Assert
        Assert.Equal(
            "Failed to load phases.",
            cut.Find("[data-testid='phase-library-load-error-SNN-AAAAAA']").TextContent.Trim()
        );
    }

    [Fact]
    public void Render_RowIsSaving_DisablesNameInput()
    {
        // Arrange
        var row = CreateRow();

        // Act
        var cut = RenderRow(row, isSaving: true);

        // Assert
        Assert.True(
            cut.Find("[data-testid='session-name-input-SNN-AAAAAA']").HasAttribute("disabled")
        );
    }
}
