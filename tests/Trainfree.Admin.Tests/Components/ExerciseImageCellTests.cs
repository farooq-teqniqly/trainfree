using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Trainfree.Admin.Components;

namespace Trainfree.Admin.Tests.Components;

public sealed class ExerciseImageCellTests : BunitContext
{
    private IRenderedComponent<ExerciseImageCell> RenderCell(
        string? src = null,
        string? error = null,
        Action<InputFileChangeEventArgs>? onFilePicked = null,
        Action? onDelete = null
    ) =>
        Render<ExerciseImageCell>(p =>
            p.Add(c => c.RowId, "EXR-AAAAAA")
                .Add(c => c.ExerciseName, "Bodyweight Squat")
                .Add(c => c.Src, src)
                .Add(c => c.Error, error)
                .Add(c => c.OnFilePicked, e => onFilePicked?.Invoke(e))
                .Add(c => c.OnDelete, () => onDelete?.Invoke())
        );

    [Fact]
    public void Render_NoImage_ShowsPlaceholderWithoutReplaceOrDelete()
    {
        // Arrange / Act
        var cut = RenderCell();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='image-placeholder-EXR-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='image-thumb-EXR-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='image-replace-EXR-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='image-delete-EXR-AAAAAA']"));
    }

    [Fact]
    public void Render_HasImage_ShowsThumbnailWithReplaceAndDeleteAndNoPlaceholder()
    {
        // Arrange / Act
        var cut = RenderCell(src: "http://localhost/api/exercises/EXR-AAAAAA/image?v=k1");

        // Assert
        var thumb = cut.Find("[data-testid='image-thumb-EXR-AAAAAA']");
        Assert.Equal(
            "http://localhost/api/exercises/EXR-AAAAAA/image?v=k1",
            thumb.GetAttribute("src")
        );
        Assert.NotNull(cut.Find("[data-testid='image-replace-EXR-AAAAAA']"));
        Assert.NotNull(cut.Find("[data-testid='image-delete-EXR-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='image-placeholder-EXR-AAAAAA']"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_EitherState_EveryControlHasAnAccessibleName(bool hasImage)
    {
        // Arrange / Act
        var cut = RenderCell(src: hasImage ? "http://localhost/api/x" : null);

        // Assert
        var controls = cut.FindAll("label, button, a, input");
        Assert.NotEmpty(controls);
        Assert.All(
            controls,
            c =>
            {
                var hasName =
                    !string.IsNullOrWhiteSpace(c.GetAttribute("title"))
                    || !string.IsNullOrWhiteSpace(c.GetAttribute("aria-label"))
                    || !string.IsNullOrWhiteSpace(c.QuerySelector(".visually-hidden")?.TextContent)
                    || c.TagName.Equals("INPUT", StringComparison.OrdinalIgnoreCase);
                Assert.True(hasName, c.OuterHtml);
            }
        );
        Assert.All(
            cut.FindAll("img"),
            i => Assert.False(string.IsNullOrWhiteSpace(i.GetAttribute("alt")))
        );
    }

    [Fact]
    public void Render_Always_FileInputAcceptsOnlyJpegAndPngAndIsLabelledByPickers()
    {
        // Arrange / Act
        var cut = RenderCell();

        // Assert
        var input = cut.Find("[data-testid='image-input-EXR-AAAAAA']");
        Assert.Equal("image/jpeg,image/png", input.GetAttribute("accept"));
        var label = cut.Find("[data-testid='image-placeholder-EXR-AAAAAA']");
        Assert.Equal(input.Id, label.GetAttribute("for"));
    }

    [Fact]
    public void Render_HasImage_ReplaceIsLabelForTheFileInput()
    {
        // Arrange / Act
        var cut = RenderCell(src: "http://localhost/api/x");

        // Assert
        var input = cut.Find("[data-testid='image-input-EXR-AAAAAA']");
        var label = cut.Find("[data-testid='image-replace-EXR-AAAAAA']");
        Assert.Equal(input.Id, label.GetAttribute("for"));
    }

    [Fact]
    public async Task FilePicked_FileChosen_InvokesOnFilePickedAndResetsTheInput()
    {
        // Arrange
        InputFileChangeEventArgs? received = null;
        var cut = RenderCell(onFilePicked: e => received = e);
        var firstInput = cut.FindComponent<InputFile>();
        var firstInputId = firstInput.ComponentId;

        // Act
        await cut.InvokeAsync(() =>
            firstInput.UploadFiles(
                InputFileContent.CreateFromBinary([1, 2, 3], "a.png", null, "image/png")
            )
        );

        // Assert
        Assert.NotNull(received);
        Assert.Equal("a.png", received.File.Name);
        Assert.NotEqual(firstInputId, cut.FindComponent<InputFile>().ComponentId);
    }

    [Fact]
    public async Task DeleteImage_Clicked_InvokesOnDelete()
    {
        // Arrange
        var deleted = false;
        var cut = RenderCell(src: "http://localhost/api/x", onDelete: () => deleted = true);

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-delete-EXR-AAAAAA']").Click());

        // Assert
        Assert.True(deleted);
    }

    [Fact]
    public void Render_HasImage_ThumbnailOpensLargerPreviewInNewTab()
    {
        // Arrange / Act
        var cut = RenderCell(src: "http://localhost/api/x");

        // Assert
        var link = cut.Find("[data-testid='image-open-EXR-AAAAAA']");
        Assert.Equal("http://localhost/api/x", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Contains("noopener", link.GetAttribute("rel"), StringComparison.Ordinal);
    }

    [Fact]
    public void Render_ErrorSet_ShowsErrorUnderTheSlot()
    {
        // Arrange / Act
        var cut = RenderCell(error: "Only JPG and PNG are supported");

        // Assert
        Assert.Equal(
            "Only JPG and PNG are supported",
            cut.Find("[data-testid='image-error-EXR-AAAAAA']").TextContent.Trim()
        );
    }

    [Fact]
    public void Render_NoError_ShowsNoErrorElement()
    {
        // Arrange / Act
        var cut = RenderCell();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='image-error-EXR-AAAAAA']"));
    }
}
