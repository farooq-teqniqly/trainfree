using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Trainfree.Admin.Components;

namespace Trainfree.Admin.Tests.Components;

public sealed class ImageStagingModalTests : BunitContext
{
    private int _uploads;
    private int _cancels;
    private InputFileChangeEventArgs? _picked;

    public ImageStagingModalTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private IRenderedComponent<ImageStagingModal> RenderModal(
        string? currentSrc = null,
        bool isUploading = false,
        string? error = null
    ) =>
        Render<ImageStagingModal>(p =>
            p.Add(c => c.ExerciseName, "Bodyweight Squat")
                .Add(c => c.PreviewSrc, "data:image/png;base64,AAAA")
                .Add(c => c.CurrentSrc, currentSrc)
                .Add(c => c.IsUploading, isUploading)
                .Add(c => c.Error, error)
                .Add(c => c.OnUpload, () => _uploads++)
                .Add(c => c.OnCancel, () => _cancels++)
                .Add(c => c.OnFilePicked, e => _picked = e)
        );

    [Fact]
    public void Render_Always_ShowsLabelledModalDialogWithContainFitPreview()
    {
        // Arrange / Act
        var cut = RenderModal();

        // Assert
        var dialog = cut.Find("[data-testid='image-modal']");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        var titleId = dialog.GetAttribute("aria-labelledby");
        Assert.Contains(
            "Bodyweight Squat",
            cut.Find($"#{titleId}").TextContent,
            StringComparison.Ordinal
        );
        var preview = cut.Find("[data-testid='image-modal-preview']");
        Assert.Equal("data:image/png;base64,AAAA", preview.GetAttribute("src"));
        Assert.Contains("image-preview", preview.ClassList);
        Assert.False(string.IsNullOrWhiteSpace(preview.GetAttribute("alt")));
    }

    [Fact]
    public void Render_Always_OffersUploadChooseDifferentFileAndCancelWithNames()
    {
        // Arrange / Act
        var cut = RenderModal();

        // Assert
        Assert.Equal("Upload", cut.Find("[data-testid='image-modal-upload']").TextContent.Trim());
        Assert.Equal(
            "Choose different file",
            cut.Find("[data-testid='image-modal-choose']").TextContent.Trim()
        );
        Assert.Equal("Cancel", cut.Find("[data-testid='image-modal-cancel']").TextContent.Trim());
        var input = cut.Find("[data-testid='image-modal-input']");
        Assert.Equal("image/jpeg,image/png", input.GetAttribute("accept"));
        Assert.Equal(input.Id, cut.Find("[data-testid='image-modal-choose']").GetAttribute("for"));
    }

    [Fact]
    public async Task Upload_Clicked_InvokesOnUploadOnly()
    {
        // Arrange
        var cut = RenderModal();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-modal-upload']").Click());

        // Assert
        Assert.Equal(1, _uploads);
        Assert.Equal(0, _cancels);
    }

    [Fact]
    public async Task Cancel_Clicked_InvokesOnCancel()
    {
        // Arrange
        var cut = RenderModal();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-modal-cancel']").Click());

        // Assert
        Assert.Equal(1, _cancels);
        Assert.Equal(0, _uploads);
    }

    [Fact]
    public async Task Escape_Pressed_InvokesOnCancel()
    {
        // Arrange
        var cut = RenderModal();

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='image-modal']")
                .KeyDown(new KeyboardEventArgs { Key = "Escape" })
        );

        // Assert
        Assert.Equal(1, _cancels);
    }

    [Fact]
    public async Task OtherKey_Pressed_DoesNotInvokeOnCancel()
    {
        // Arrange
        var cut = RenderModal();

        // Act
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='image-modal']").KeyDown(new KeyboardEventArgs { Key = "a" })
        );

        // Assert
        Assert.Equal(0, _cancels);
    }

    [Fact]
    public async Task Backdrop_ClickedOutsideDialog_InvokesOnCancel()
    {
        // Arrange
        var cut = RenderModal();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-modal']").Click());

        // Assert
        Assert.Equal(1, _cancels);
    }

    [Fact]
    public async Task DialogBody_Clicked_DoesNotInvokeOnCancel()
    {
        // Arrange
        var cut = RenderModal();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-modal-body']").Click());

        // Assert
        Assert.Equal(0, _cancels);
    }

    [Fact]
    public void Render_CurrentImageGiven_ShowsCurrentBesideNew()
    {
        // Arrange / Act
        var cut = RenderModal(currentSrc: "http://api.test/api/exercises/EXR-AAAAAA/image?v=k1");

        // Assert
        var current = cut.Find("[data-testid='image-modal-current']");
        Assert.Equal(
            "http://api.test/api/exercises/EXR-AAAAAA/image?v=k1",
            current.GetAttribute("src")
        );
        Assert.False(string.IsNullOrWhiteSpace(current.GetAttribute("alt")));
        Assert.NotNull(cut.Find("[data-testid='image-modal-preview']"));
    }

    [Fact]
    public void Render_NoCurrentImage_ShowsOnlyNew()
    {
        // Arrange / Act
        var cut = RenderModal();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='image-modal-current']"));
    }

    [Fact]
    public void Render_NotUploading_ControlsAreEnabled()
    {
        // Arrange / Act
        var cut = RenderModal();

        // Assert
        Assert.False(cut.Find("[data-testid='image-modal-upload']").HasAttribute("disabled"));
        Assert.False(cut.Find("[data-testid='image-modal-cancel']").HasAttribute("disabled"));
        Assert.False(cut.Find("[data-testid='image-modal-input']").HasAttribute("disabled"));
        Assert.DoesNotContain("disabled", cut.Find("[data-testid='image-modal-choose']").ClassList);
    }

    [Fact]
    public void Render_Uploading_ShowsSpinnerAndUploadingLabelOnUploadButton()
    {
        // Arrange / Act
        var cut = RenderModal(isUploading: true);

        // Assert
        var button = cut.Find("[data-testid='image-modal-upload']");
        Assert.NotNull(button.QuerySelector("[data-testid='image-modal-spinner']"));
        Assert.Contains("Uploading", button.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_NotUploading_ShowsNoSpinner()
    {
        // Arrange / Act
        var cut = RenderModal();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='image-modal-spinner']"));
        Assert.DoesNotContain(
            "Uploading",
            cut.Find("[data-testid='image-modal-upload']").TextContent,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Render_Uploading_DisablesEveryControl()
    {
        // Arrange / Act
        var cut = RenderModal(isUploading: true);

        // Assert
        Assert.True(cut.Find("[data-testid='image-modal-upload']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='image-modal-cancel']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='image-modal-input']").HasAttribute("disabled"));
        Assert.Contains("disabled", cut.Find("[data-testid='image-modal-choose']").ClassList);
    }

    [Fact]
    public async Task DismissGestures_WhileUploading_AreIgnored()
    {
        // Arrange
        var cut = RenderModal(isUploading: true);
        var dialog = cut.Find("[data-testid='image-modal']");

        // Act
        await cut.InvokeAsync(() => dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" }));
        await cut.InvokeAsync(() => dialog.Click());

        // Assert
        Assert.Equal(0, _cancels);
    }

    [Fact]
    public void Render_ErrorGiven_ShowsErrorAndKeepsControlsEnabled()
    {
        // Arrange / Act
        var cut = RenderModal(error: "Image must be 1 MB or smaller");

        // Assert
        Assert.Equal(
            "Image must be 1 MB or smaller",
            cut.Find("[data-testid='image-modal-error']").TextContent.Trim()
        );
        Assert.False(cut.Find("[data-testid='image-modal-upload']").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_NoError_ShowsNoErrorElement()
    {
        // Arrange / Act
        var cut = RenderModal();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='image-modal-error']"));
    }

    [Fact]
    public async Task ChooseDifferentFile_FileChosen_InvokesOnFilePickedWithoutUploading()
    {
        // Arrange
        var cut = RenderModal();

        // Act
        await cut.InvokeAsync(() =>
            cut.FindComponent<InputFile>()
                .UploadFiles(InputFileContent.CreateFromBinary([1], "b.png", null, "image/png"))
        );

        // Assert
        Assert.NotNull(_picked);
        Assert.Equal("b.png", _picked.File.Name);
        Assert.Equal(0, _uploads);
    }
}
