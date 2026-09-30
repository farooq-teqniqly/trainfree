using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Pages;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Admin;

public sealed class ExercisesPageTests : BunitContext
{
    private readonly IExercisesApiClient _apiClient = Substitute.For<IExercisesApiClient>();

    private readonly IImageResizer _resizer = Substitute.For<IImageResizer>();

    public ExercisesPageTests()
    {
        Services.AddSingleton(_apiClient);
        Services.AddSingleton(_resizer);
        Services.AddSingleton(_ => new HttpClient
        {
            BaseAddress = new Uri("http://api.test/api/"),
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Render_ExistingExercises_ImageColumnHeaderIsLeftOfName()
    {
        // Arrange
        _apiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns([new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat")]);

        // Act
        var cut = Render<Exercises>();

        // Assert
        var headers = cut.FindAll("thead th").Select(h => h.TextContent.Trim()).ToList();
        Assert.Equal(["Image", "Name"], headers);
        var cells = cut.FindAll("tbody tr td");
        Assert.NotNull(cells[0].QuerySelector("[data-testid='image-placeholder-EXR-AAAAAA']"));
        Assert.NotNull(cells[1].QuerySelector("[data-testid='name-input-EXR-AAAAAA']"));
    }

    [Fact]
    public void Render_ExerciseWithoutImage_ShowsPlaceholderOnly()
    {
        // Arrange
        _apiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns([new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat")]);

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='image-placeholder-EXR-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='image-replace-EXR-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='image-delete-EXR-AAAAAA']"));
    }

    [Fact]
    public void Render_ExerciseWithImage_ShowsThumbnailResolvedAgainstApiBaseWithReplaceAndDelete()
    {
        // Arrange
        _apiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns([
                new ExerciseSummary(
                    ExerciseId.Parse("EXR-AAAAAA"),
                    "Bodyweight Squat",
                    "/api/exercises/EXR-AAAAAA/image?v=k1"
                ),
                new ExerciseSummary(ExerciseId.Parse("EXR-BBBBBB"), "Skater Jump"),
            ]);

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.Equal(
            "http://api.test/api/exercises/EXR-AAAAAA/image?v=k1",
            cut.Find("[data-testid='image-thumb-EXR-AAAAAA']").GetAttribute("src")
        );
        Assert.NotNull(cut.Find("[data-testid='image-replace-EXR-AAAAAA']"));
        Assert.NotNull(cut.Find("[data-testid='image-delete-EXR-AAAAAA']"));
        Assert.NotNull(cut.Find("[data-testid='image-placeholder-EXR-BBBBBB']"));
    }

    [Fact]
    public void OnInitialized_ServerReturnsTheAccessLoginPage_ShowsTheLoadErrorInsteadOfFailing()
    {
        // Arrange
        _apiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns<IReadOnlyList<ExerciseSummary>>(_ =>
                throw new JsonException("'<' is an invalid start of a value.")
            );

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid=load-exercises-error]"));
        Assert.Empty(cut.FindAll("tbody tr"));
        Assert.Empty(cut.FindAll("[data-testid='exercises-empty']"));
    }

    [Fact]
    public void OnInitialized_ServerReturnsMalformedExerciseId_ShowsTheLoadErrorInsteadOfFailing()
    {
        // Arrange
        _apiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns<IReadOnlyList<ExerciseSummary>>(_ =>
                throw new FormatException("Exercise id 'bad-id' is not in the expected format.")
            );

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid=load-exercises-error]"));
        Assert.Empty(cut.FindAll("tbody tr"));
        Assert.Empty(cut.FindAll("[data-testid='exercises-empty']"));
    }

    [Fact]
    public void OnInitialized_FetchNotYetResolved_RendersSkeletonRowsNotEmptyStateOrTable()
    {
        // Arrange
        var tcs = new TaskCompletionSource<IReadOnlyList<ExerciseSummary>>();
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns(tcs.Task);

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.NotEmpty(cut.FindAll(".placeholder"));
        Assert.Empty(cut.FindAll("[data-testid='exercises-empty']"));
        Assert.Empty(cut.FindAll("[data-testid^='name-input-']"));
    }

    [Fact]
    public void OnInitialized_NoExercises_ShowsEmptyState()
    {
        // Arrange
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([]);

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='exercises-empty']"));
        Assert.Empty(cut.FindAll("table"));
    }

    [Fact]
    public void OnInitialized_ExistingExercises_RendersOneRowPerExercise()
    {
        // Arrange
        _apiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns([
                new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat"),
                new ExerciseSummary(ExerciseId.Parse("EXR-BBBBBB"), "Skater Jump"),
            ]);

        // Act
        var cut = Render<Exercises>();

        // Assert
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Bodyweight Squat", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Skater Jump", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddExercise_ClickPlusExercise_AppendsRowInEditMode()
    {
        // Arrange
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([]);
        var created = new ExerciseSummary(ExerciseId.Parse("EXR-CCCCCC"), "New Exercise");
        _apiClient
            .CreateExerciseAsync("New Exercise", CancellationToken.None)
            .Returns(new CreateExerciseSucceeded(created));
        var cut = Render<Exercises>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-empty']").Click());

        // Assert
        await _apiClient.Received(1).CreateExerciseAsync("New Exercise", CancellationToken.None);
        var input = cut.Find("[data-testid='name-input-EXR-CCCCCC']");
        Assert.True(input.HasAttribute("autofocus"));
    }

    [Fact]
    public async Task AddExercise_ServerRejectsDuplicateName_ShowsErrorAndAddsNoRow()
    {
        // Arrange
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([]);
        _apiClient
            .CreateExerciseAsync("New Exercise", CancellationToken.None)
            .Returns(
                new CreateExerciseFailed("An exercise named \"New Exercise\" already exists.")
            );
        var cut = Render<Exercises>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-empty']").Click());

        // Assert
        Assert.Contains(
            "An exercise named \"New Exercise\" already exists.",
            cut.Markup,
            StringComparison.Ordinal
        );
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public async Task RenameExercise_NameEditedAndSaveClicked_CallsRenameAndUpdatesDisplayedName()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var renamed = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Skater Jump");
        _apiClient
            .RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                "Skater Jump",
                CancellationToken.None
            )
            .Returns(new RenameExerciseSucceeded(renamed));
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Skater Jump"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-EXR-AAAAAA']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                "Skater Jump",
                CancellationToken.None
            );
        Assert.Contains("Skater Jump", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task RenameExercise_NameFailsLengthBound_ShowsErrorAndDoesNotCallApi()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Ab"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-EXR-AAAAAA']").Click());

        // Assert
        await _apiClient
            .DidNotReceive()
            .RenameExerciseAsync(
                Arg.Any<ExerciseId>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
        Assert.NotEmpty(cut.FindAll("[data-testid='name-error-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task RenameExercise_ServerRejects_ShowsErrorWithoutThrowing()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        _apiClient
            .RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                "Skater Jump",
                CancellationToken.None
            )
            .Returns(new RenameExerciseFailed("name must be between 4 and 100 characters"));
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Skater Jump"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-EXR-AAAAAA']").Click());

        // Assert
        Assert.Contains(
            "name must be between 4 and 100 characters",
            cut.Markup,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task RenameExercise_EnterKeyOnDirtyRow_CallsRenameAndUpdatesDisplayedName()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var renamed = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Skater Jump");
        _apiClient
            .RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                "Skater Jump",
                CancellationToken.None
            )
            .Returns(new RenameExerciseSucceeded(renamed));
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Skater Jump"));

        // Act
        await cut.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        // Assert
        await _apiClient
            .Received(1)
            .RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                "Skater Jump",
                CancellationToken.None
            );
        Assert.Contains("Skater Jump", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task RenameExercise_EnterKeyOnCleanRow_DoesNotCallApi()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "Enter" }));

        // Assert
        await _apiClient
            .DidNotReceive()
            .RenameExerciseAsync(
                Arg.Any<ExerciseId>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task RenameExercise_EnterKeyRepeatedWhileSaveInFlight_CallsRenameOnlyOnce()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var renamed = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Skater Jump");
        var tcs = new TaskCompletionSource<RenameExerciseOutcome>();
        _apiClient
            .RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                "Skater Jump",
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Skater Jump"));

        // Act
        var firstKeyDown = cut.InvokeAsync(() =>
            input.KeyDown(new KeyboardEventArgs { Key = "Enter" })
        );
        await cut.InvokeAsync(() => input.KeyDown(new KeyboardEventArgs { Key = "Enter" }));
        tcs.SetResult(new RenameExerciseSucceeded(renamed));
        await firstKeyDown;

        // Assert
        await _apiClient
            .Received(1)
            .RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                "Skater Jump",
                CancellationToken.None
            );
    }

    [Fact]
    public async Task RenameExercise_SaveInFlight_DisablesNameInputSoLaterEditsAreNotSilentlyLost()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var tcs = new TaskCompletionSource<RenameExerciseOutcome>();
        _apiClient
            .RenameExerciseAsync(
                ExerciseId.Parse("EXR-AAAAAA"),
                "Skater Jump",
                CancellationToken.None
            )
            .Returns(tcs.Task);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Skater Jump"));

        // Act
        var saveTask = cut.InvokeAsync(() =>
            input.KeyDown(new KeyboardEventArgs { Key = "Enter" })
        );

        // Assert: input is disabled while the save is in flight, so a second edit
        // cannot be typed and silently discarded when the first response lands
        Assert.True(cut.Find("[data-testid='name-input-EXR-AAAAAA']").HasAttribute("disabled"));

        await cut.InvokeAsync(() =>
            tcs.SetResult(new RenameExerciseSucceeded(exercise with { Name = "Skater Jump" }))
        );
        await saveTask;
        Assert.False(cut.Find("[data-testid='name-input-EXR-AAAAAA']").HasAttribute("disabled"));
    }

    [Fact]
    public void SaveButton_NoUnsavedChanges_IsNotShown()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='save-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task SaveButton_NameEdited_BecomesVisible()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Skater Jump"));

        // Assert
        Assert.Single(cut.FindAll("[data-testid='save-EXR-AAAAAA']"));
    }

    [Fact]
    public void RevertButton_NoUnsavedChanges_IsNotShown()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='revert-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task RevertButton_NameEdited_BecomesVisible()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Skater Jump"));

        // Assert
        Assert.Single(cut.FindAll("[data-testid='revert-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task RevertExercise_ClickRevert_RestoresOriginalNameAndHidesButtons()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Skater Jump"));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='revert-EXR-AAAAAA']").Click());

        // Assert
        Assert.Contains("value=\"Bodyweight Squat\"", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-EXR-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='revert-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task RevertExercise_NameShowingValidationError_ClearsError()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        var cut = Render<Exercises>();
        var input = cut.Find("[data-testid='name-input-EXR-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Ab"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-EXR-AAAAAA']").Click());
        Assert.NotEmpty(cut.FindAll("[data-testid='name-error-EXR-AAAAAA']"));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='revert-EXR-AAAAAA']").Click());

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='name-error-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task DeleteExercise_ClickDelete_CallsDeleteAndRemovesRow()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        _apiClient
            .DeleteExerciseAsync(ExerciseId.Parse("EXR-AAAAAA"), CancellationToken.None)
            .Returns(new DeleteExerciseSucceeded());
        var cut = Render<Exercises>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-EXR-AAAAAA']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .DeleteExerciseAsync(ExerciseId.Parse("EXR-AAAAAA"), CancellationToken.None);
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public async Task DeleteExercise_ServerRejects_ShowsErrorAndKeepsRowWithoutThrowing()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        _apiClient
            .DeleteExerciseAsync(ExerciseId.Parse("EXR-AAAAAA"), CancellationToken.None)
            .Returns(new DeleteExerciseFailed("Request failed with status 500."));
        var cut = Render<Exercises>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-EXR-AAAAAA']").Click());

        // Assert
        Assert.Contains("Request failed with status 500.", cut.Markup, StringComparison.Ordinal);
        Assert.Single(cut.FindAll("tbody tr"));
    }

    [Fact]
    public async Task DeleteExercise_ServerRejectsAsInUse_ShowsRejectionOnRowWithoutRemovingIt()
    {
        // Arrange
        var exercise = new ExerciseSummary(ExerciseId.Parse("EXR-AAAAAA"), "Bodyweight Squat");
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([exercise]);
        _apiClient
            .DeleteExerciseAsync(ExerciseId.Parse("EXR-AAAAAA"), CancellationToken.None)
            .Returns(
                new DeleteExerciseFailed(
                    "Exercise \"EXR-AAAAAA\" is referenced by at least one program exercise and cannot be deleted."
                )
            );
        var cut = Render<Exercises>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-EXR-AAAAAA']").Click());

        // Assert
        Assert.Equal(
            "Exercise \"EXR-AAAAAA\" is referenced by at least one program exercise and cannot be deleted.",
            cut.Find("[data-testid='name-error-EXR-AAAAAA']").TextContent.Trim()
        );
        Assert.Single(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_LoadFails_ShowsErrorWithoutThrowing()
    {
        // Arrange
        _apiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns<Task<IReadOnlyList<ExerciseSummary>>>(_ =>
                throw new HttpRequestException("simulated failure")
            );

        // Act
        var cut = Render<Exercises>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='load-exercises-error']"));
        Assert.Empty(cut.FindAll("tbody tr"));
        Assert.Empty(cut.FindAll("[data-testid='exercises-empty']"));
    }

    private static readonly ExerciseId SquatId = ExerciseId.Parse("EXR-AAAAAA");
    private const string SquatImagePath = "/api/exercises/EXR-AAAAAA/image?v=k1";

    private IRenderedComponent<Exercises> RenderWithSquat(string? imageUrl = null)
    {
        _apiClient
            .GetExercisesAsync(CancellationToken.None)
            .Returns([new ExerciseSummary(SquatId, "Bodyweight Squat", imageUrl)]);
        return Render<Exercises>();
    }

    private static async Task PickAsync(
        IRenderedComponent<Exercises> cut,
        string testId,
        string fileName,
        string contentType,
        byte[] content
    )
    {
        var input = cut.FindComponents<InputFile>()
            .Single(c =>
                string.Equals(
                    c.Instance.AdditionalAttributes?["data-testid"] as string,
                    testId,
                    StringComparison.Ordinal
                )
            );
        await cut.InvokeAsync(() =>
            input.UploadFiles(
                InputFileContent.CreateFromBinary(content, fileName, null, contentType)
            )
        );
    }

    private static Task PickRowImageAsync(
        IRenderedComponent<Exercises> cut,
        string fileName = "squat.png",
        string contentType = "image/png",
        byte[]? content = null
    ) => PickAsync(cut, "image-input-EXR-AAAAAA", fileName, contentType, content ?? PngBytes(16));

    private static byte[] PngBytes(int length)
    {
        var bytes = new byte[length];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            .AsSpan(0, Math.Min(8, length))
            .CopyTo(bytes);
        return bytes;
    }

    private StagedImage StubResizer(byte[] resized, string contentType = "image/png")
    {
        var staged = new StagedImage(resized, contentType);
        _resizer
            .ResizeAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(staged);
        return staged;
    }

    private async Task AssertNoUploadAsync() =>
        await _apiClient
            .DidNotReceive()
            .UploadExerciseImageAsync(
                Arg.Any<ExerciseId>(),
                Arg.Any<StagedImage>(),
                Arg.Any<CancellationToken>()
            );

    [Fact]
    public async Task PickImage_ValidPng_OpensPreviewModalWithProcessedImageAndSendsNoRequest()
    {
        // Arrange
        var staged = StubResizer([9, 8, 7]);
        var cut = RenderWithSquat();

        // Act
        await PickRowImageAsync(cut, content: PngBytes(16));

        // Assert
        await _resizer
            .Received(1)
            .ResizeAsync(
                Arg.Is<byte[]>(b => b.SequenceEqual(PngBytes(16))),
                "image/png",
                Arg.Any<CancellationToken>()
            );
        Assert.Equal(
            $"data:image/png;base64,{Convert.ToBase64String(staged.Content.Span)}",
            cut.Find("[data-testid='image-modal-preview']").GetAttribute("src")
        );
        Assert.Empty(cut.FindAll("[data-testid='image-modal-current']"));
        await AssertNoUploadAsync();
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    public async Task PickImage_UnsupportedType_ShowsTypeErrorOnRowWithoutModalOrRequest(
        string contentType
    )
    {
        // Arrange
        var cut = RenderWithSquat();

        // Act
        await PickRowImageAsync(cut, "squat.gif", contentType);

        // Assert
        Assert.Equal(
            "Only JPG and PNG are supported",
            cut.Find("[data-testid='image-error-EXR-AAAAAA']").TextContent.Trim()
        );
        Assert.Empty(cut.FindAll("[data-testid='image-modal']"));
        await _resizer
            .DidNotReceive()
            .ResizeAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await AssertNoUploadAsync();
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    public async Task PickImage_NonImageBytesWithImageContentType_ShowsTypeErrorOnRowWithoutModalOrRequest(
        string contentType
    )
    {
        // Arrange
        var cut = RenderWithSquat();

        // Act
        await PickRowImageAsync(cut, "notes.png", contentType, "just some text"u8.ToArray());

        // Assert
        Assert.Equal(
            "Only JPG and PNG are supported",
            cut.Find("[data-testid='image-error-EXR-AAAAAA']").TextContent.Trim()
        );
        Assert.Empty(cut.FindAll("[data-testid='image-modal']"));
        await _resizer
            .DidNotReceive()
            .ResizeAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await AssertNoUploadAsync();
    }

    [Fact]
    public async Task ChooseDifferentFile_NonImageBytes_KeepsPreviousPreviewAndShowsTypeErrorInModal()
    {
        // Arrange
        var staged = StubResizer([9]);
        var cut = RenderWithSquat();
        await PickRowImageAsync(cut);
        _resizer.ClearReceivedCalls();

        // Act
        await PickAsync(
            cut,
            "image-modal-input",
            "other.png",
            "image/png",
            "just some text"u8.ToArray()
        );

        // Assert
        Assert.Equal(
            "Only JPG and PNG are supported",
            cut.Find("[data-testid='image-modal-error']").TextContent.Trim()
        );
        Assert.Equal(
            $"data:image/png;base64,{Convert.ToBase64String(staged.Content.Span)}",
            cut.Find("[data-testid='image-modal-preview']").GetAttribute("src")
        );
        await _resizer
            .DidNotReceive()
            .ResizeAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PickImage_FileOverOneMegabyte_ShowsSizeErrorOnRowWithoutModalOrRequest()
    {
        // Arrange
        var cut = RenderWithSquat();

        // Act
        await PickRowImageAsync(cut, content: PngBytes(1_048_577));

        // Assert
        Assert.Equal(
            "Image must be 1 MB or smaller",
            cut.Find("[data-testid='image-error-EXR-AAAAAA']").TextContent.Trim()
        );
        Assert.Empty(cut.FindAll("[data-testid='image-modal']"));
        await _resizer
            .DidNotReceive()
            .ResizeAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await AssertNoUploadAsync();
    }

    [Fact]
    public async Task PickImage_ResizeFails_ShowsErrorOnRowWithoutModal()
    {
        // Arrange
        _resizer
            .ResizeAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<StagedImage>(_ => throw new JSException("decode failed"));
        var cut = RenderWithSquat();

        // Act
        await PickRowImageAsync(cut);

        // Assert
        Assert.Equal(
            "Could not process that image. Try a different file.",
            cut.Find("[data-testid='image-error-EXR-AAAAAA']").TextContent.Trim()
        );
        Assert.Empty(cut.FindAll("[data-testid='image-modal']"));
    }

    [Fact]
    public async Task PickImage_ResizedOutputOverOneMegabyte_ShowsSizeErrorOnRowAndStagesNothing()
    {
        // Arrange
        StubResizer(new byte[1_048_577]);
        var cut = RenderWithSquat();

        // Act
        await PickRowImageAsync(cut);

        // Assert
        Assert.Equal(
            "Image must be 1 MB or smaller",
            cut.Find("[data-testid='image-error-EXR-AAAAAA']").TextContent.Trim()
        );
        Assert.Empty(cut.FindAll("[data-testid='image-modal']"));
        await AssertNoUploadAsync();
    }

    [Fact]
    public async Task ChooseDifferentFile_ResizedOutputOverOneMegabyte_KeepsPreviousPreviewAndShowsErrorInModal()
    {
        // Arrange
        var staged = StubResizer([9]);
        var cut = RenderWithSquat();
        await PickRowImageAsync(cut);
        _resizer
            .ResizeAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StagedImage(new byte[1_048_577], "image/png"));

        // Act
        await PickAsync(cut, "image-modal-input", "other.png", "image/png", PngBytes(16));

        // Assert
        Assert.Equal(
            "Image must be 1 MB or smaller",
            cut.Find("[data-testid='image-modal-error']").TextContent.Trim()
        );
        Assert.Equal(
            $"data:image/png;base64,{Convert.ToBase64String(staged.Content.Span)}",
            cut.Find("[data-testid='image-modal-preview']").GetAttribute("src")
        );
    }

    [Fact]
    public async Task PickImage_AfterEarlierRejection_ClearsTheRowError()
    {
        // Arrange
        StubResizer([9]);
        var cut = RenderWithSquat();
        await PickRowImageAsync(cut, "squat.gif", "image/gif");
        Assert.NotEmpty(cut.FindAll("[data-testid='image-error-EXR-AAAAAA']"));

        // Act
        await PickRowImageAsync(cut);

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='image-error-EXR-AAAAAA']"));
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("escape")]
    [InlineData("outside")]
    public async Task StagedImage_Dismissed_DiscardsItLeavesRowUnchangedAndSendsNoRequest(
        string gesture
    )
    {
        // Arrange
        StubResizer([9]);
        var cut = RenderWithSquat();
        await PickRowImageAsync(cut);
        var dialog = cut.Find("[data-testid='image-modal']");

        // Act
        await cut.InvokeAsync(() =>
        {
            switch (gesture)
            {
                case "cancel":
                    cut.Find("[data-testid='image-modal-cancel']").Click();
                    break;
                case "escape":
                    dialog.KeyDown(new KeyboardEventArgs { Key = "Escape" });
                    break;
                default:
                    dialog.Click();
                    break;
            }
        });

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='image-modal']"));
        Assert.NotNull(cut.Find("[data-testid='image-placeholder-EXR-AAAAAA']"));
        await AssertNoUploadAsync();
    }

    [Fact]
    public async Task UploadImage_ConfirmedAndSucceeds_SendsStagedImageClosesModalAndShowsNewThumbnail()
    {
        // Arrange
        var staged = StubResizer([9, 8, 7]);
        _apiClient
            .UploadExerciseImageAsync(
                SquatId,
                Arg.Is<StagedImage>(s => ReferenceEquals(s, staged)),
                CancellationToken.None
            )
            .Returns(
                new UploadExerciseImageSucceeded(
                    new ExerciseSummary(SquatId, "Bodyweight Squat", SquatImagePath)
                )
            );
        var cut = RenderWithSquat();
        await PickRowImageAsync(cut);

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-modal-upload']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .UploadExerciseImageAsync(
                SquatId,
                Arg.Is<StagedImage>(s => ReferenceEquals(s, staged)),
                CancellationToken.None
            );
        Assert.Empty(cut.FindAll("[data-testid='image-modal']"));
        Assert.Equal(
            "http://api.test/api/exercises/EXR-AAAAAA/image?v=k1",
            cut.Find("[data-testid='image-thumb-EXR-AAAAAA']").GetAttribute("src")
        );
    }

    [Fact]
    public async Task UploadImage_InFlight_DisablesModalControls()
    {
        // Arrange
        StubResizer([9]);
        var tcs = new TaskCompletionSource<UploadExerciseImageOutcome>();
        _apiClient
            .UploadExerciseImageAsync(
                Arg.Any<ExerciseId>(),
                Arg.Any<StagedImage>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(tcs.Task);
        var cut = RenderWithSquat();
        await PickRowImageAsync(cut);

        // Act
        var upload = cut.InvokeAsync(() => cut.Find("[data-testid='image-modal-upload']").Click());

        // Assert
        Assert.True(cut.Find("[data-testid='image-modal-upload']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='image-modal-cancel']").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid='image-modal-input']").HasAttribute("disabled"));

        await cut.InvokeAsync(() =>
            tcs.SetResult(
                new UploadExerciseImageSucceeded(
                    new ExerciseSummary(SquatId, "Bodyweight Squat", SquatImagePath)
                )
            )
        );
        await upload;
    }

    [Fact]
    public async Task UploadImage_WorkerRejects_KeepsModalOpenShowsErrorAndReEnablesControls()
    {
        // Arrange
        StubResizer([9]);
        _apiClient
            .UploadExerciseImageAsync(
                Arg.Any<ExerciseId>(),
                Arg.Any<StagedImage>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new UploadExerciseImageFailed("Image must be 1 MB or smaller"));
        var cut = RenderWithSquat();
        await PickRowImageAsync(cut);

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-modal-upload']").Click());

        // Assert
        Assert.Equal(
            "Image must be 1 MB or smaller",
            cut.Find("[data-testid='image-modal-error']").TextContent.Trim()
        );
        Assert.False(cut.Find("[data-testid='image-modal-upload']").HasAttribute("disabled"));
        Assert.False(cut.Find("[data-testid='image-modal-cancel']").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-testid='image-thumb-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task ChooseDifferentFile_ValidFile_ReplacesPreviewWithoutUploading()
    {
        // Arrange
        _resizer
            .ResizeAsync(
                Arg.Is<byte[]>(b => b.SequenceEqual(PngBytes(8))),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new StagedImage([10], "image/png"));
        _resizer
            .ResizeAsync(
                Arg.Is<byte[]>(b => b.SequenceEqual(PngBytes(9))),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new StagedImage([20], "image/png"));
        var cut = RenderWithSquat();
        await PickRowImageAsync(cut, content: PngBytes(8));

        // Act
        await PickAsync(cut, "image-modal-input", "other.png", "image/png", PngBytes(9));

        // Assert
        Assert.Equal(
            $"data:image/png;base64,{Convert.ToBase64String(new byte[] { 20 })}",
            cut.Find("[data-testid='image-modal-preview']").GetAttribute("src")
        );
        await AssertNoUploadAsync();
    }

    [Fact]
    public async Task ChooseDifferentFile_InvalidFile_KeepsPreviousPreviewAndShowsErrorInModal()
    {
        // Arrange
        var staged = StubResizer([9]);
        var cut = RenderWithSquat();
        await PickRowImageAsync(cut);

        // Act
        await PickAsync(cut, "image-modal-input", "other.gif", "image/gif", [2]);

        // Assert
        Assert.Equal(
            "Only JPG and PNG are supported",
            cut.Find("[data-testid='image-modal-error']").TextContent.Trim()
        );
        Assert.Equal(
            $"data:image/png;base64,{Convert.ToBase64String(staged.Content.Span)}",
            cut.Find("[data-testid='image-modal-preview']").GetAttribute("src")
        );
        Assert.Empty(cut.FindAll("[data-testid='image-error-EXR-AAAAAA']"));
    }

    [Fact]
    public async Task ChooseDifferentFile_AfterUploadError_ClearsModalError()
    {
        // Arrange
        StubResizer([9]);
        _apiClient
            .UploadExerciseImageAsync(
                Arg.Any<ExerciseId>(),
                Arg.Any<StagedImage>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(new UploadExerciseImageFailed("boom"));
        var cut = RenderWithSquat();
        await PickRowImageAsync(cut);
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-modal-upload']").Click());
        Assert.NotEmpty(cut.FindAll("[data-testid='image-modal-error']"));

        // Act
        await PickAsync(cut, "image-modal-input", "other.png", "image/png", PngBytes(16));

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='image-modal-error']"));
    }

    [Fact]
    public async Task PickImage_ExerciseAlreadyHasImage_ModalShowsCurrentBesideNew()
    {
        // Arrange
        StubResizer([9]);
        var cut = RenderWithSquat(SquatImagePath);

        // Act
        await PickRowImageAsync(cut);

        // Assert
        Assert.Equal(
            "http://api.test/api/exercises/EXR-AAAAAA/image?v=k1",
            cut.Find("[data-testid='image-modal-current']").GetAttribute("src")
        );
        Assert.NotNull(cut.Find("[data-testid='image-modal-preview']"));
    }

    [Fact]
    public async Task UploadImage_ReplaceSucceeds_ThumbnailUsesTheNewUrl()
    {
        // Arrange
        StubResizer([9]);
        _apiClient
            .UploadExerciseImageAsync(
                Arg.Any<ExerciseId>(),
                Arg.Any<StagedImage>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new UploadExerciseImageSucceeded(
                    new ExerciseSummary(
                        SquatId,
                        "Bodyweight Squat",
                        "/api/exercises/EXR-AAAAAA/image?v=k2"
                    )
                )
            );
        var cut = RenderWithSquat(SquatImagePath);
        await PickRowImageAsync(cut);

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-modal-upload']").Click());

        // Assert
        Assert.Equal(
            "http://api.test/api/exercises/EXR-AAAAAA/image?v=k2",
            cut.Find("[data-testid='image-thumb-EXR-AAAAAA']").GetAttribute("src")
        );
    }

    [Fact]
    public async Task UploadImage_UnsavedNameEdit_LeavesNameEditAndSaveRevertUnchanged()
    {
        // Arrange
        StubResizer([9]);
        _apiClient
            .UploadExerciseImageAsync(
                Arg.Any<ExerciseId>(),
                Arg.Any<StagedImage>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new UploadExerciseImageSucceeded(
                    new ExerciseSummary(SquatId, "Bodyweight Squat", SquatImagePath)
                )
            );
        var cut = RenderWithSquat();
        await cut.InvokeAsync(() =>
            cut.Find("[data-testid='name-input-EXR-AAAAAA']").Input("Skater Jump")
        );

        // Act
        await PickRowImageAsync(cut);
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-modal-upload']").Click());

        // Assert
        Assert.Equal(
            "Skater Jump",
            cut.Find("[data-testid='name-input-EXR-AAAAAA']").GetAttribute("value")
        );
        Assert.Single(cut.FindAll("[data-testid='save-EXR-AAAAAA']"));
        Assert.Single(cut.FindAll("[data-testid='revert-EXR-AAAAAA']"));
        await _apiClient
            .DidNotReceive()
            .RenameExerciseAsync(
                Arg.Any<ExerciseId>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task DeleteImage_ServerSucceeds_ShowsPlaceholderAndKeepsExerciseAndName()
    {
        // Arrange
        _apiClient
            .DeleteExerciseImageAsync(SquatId, CancellationToken.None)
            .Returns(new DeleteExerciseImageSucceeded());
        var cut = RenderWithSquat(SquatImagePath);

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-delete-EXR-AAAAAA']").Click());

        // Assert
        await _apiClient.Received(1).DeleteExerciseImageAsync(SquatId, CancellationToken.None);
        Assert.NotNull(cut.Find("[data-testid='image-placeholder-EXR-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='image-thumb-EXR-AAAAAA']"));
        Assert.Single(cut.FindAll("tbody tr"));
        Assert.Equal(
            "Bodyweight Squat",
            cut.Find("[data-testid='name-input-EXR-AAAAAA']").GetAttribute("value")
        );
        await _apiClient
            .DidNotReceive()
            .DeleteExerciseAsync(Arg.Any<ExerciseId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteImage_ServerRejects_KeepsThumbnailAndShowsError()
    {
        // Arrange
        _apiClient
            .DeleteExerciseImageAsync(SquatId, CancellationToken.None)
            .Returns(new DeleteExerciseImageFailed("Request failed with status 500."));
        var cut = RenderWithSquat(SquatImagePath);

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='image-delete-EXR-AAAAAA']").Click());

        // Assert
        Assert.NotNull(cut.Find("[data-testid='image-thumb-EXR-AAAAAA']"));
        Assert.Equal(
            "Request failed with status 500.",
            cut.Find("[data-testid='image-error-EXR-AAAAAA']").TextContent.Trim()
        );
    }

    [Fact]
    public async Task AddExercise_NewRow_HasImagePlaceholder()
    {
        // Arrange
        _apiClient.GetExercisesAsync(CancellationToken.None).Returns([]);
        _apiClient
            .CreateExerciseAsync("New Exercise", CancellationToken.None)
            .Returns(
                new CreateExerciseSucceeded(
                    new ExerciseSummary(ExerciseId.Parse("EXR-CCCCCC"), "New Exercise")
                )
            );
        var cut = Render<Exercises>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-exercise-empty']").Click());

        // Assert
        Assert.NotNull(cut.Find("[data-testid='image-placeholder-EXR-CCCCCC']"));
    }
}
