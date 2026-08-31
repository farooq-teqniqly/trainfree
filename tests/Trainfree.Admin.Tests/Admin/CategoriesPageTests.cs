using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Trainfree.Admin.Admin;
using Trainfree.Admin.Pages;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Tests.Admin;

public sealed class CategoriesPageTests : BunitContext
{
    private readonly ICategoriesApiClient _apiClient = Substitute.For<ICategoriesApiClient>();

    public CategoriesPageTests() => Services.AddSingleton(_apiClient);

    [Fact]
    public void OnInitialized_ServerReturnsTheAccessLoginPage_ShowsTheLoadErrorInsteadOfFailing()
    {
        // Arrange
        _apiClient
            .GetCategoriesAsync(CancellationToken.None)
            .Returns<IReadOnlyList<CategorySummary>>(_ =>
                throw new JsonException("'<' is an invalid start of a value.")
            );

        // Act
        var cut = Render<Categories>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid=load-categories-error]"));
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_NoCategories_ShowsEmptyState()
    {
        // Arrange
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([]);

        // Act
        var cut = Render<Categories>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='categories-empty']"));
        Assert.Empty(cut.FindAll("table"));
    }

    [Fact]
    public void OnInitialized_ExistingCategories_RendersOneRowPerCategory()
    {
        // Arrange
        _apiClient
            .GetCategoriesAsync(CancellationToken.None)
            .Returns([
                new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up"),
                new CategorySummary(CategoryId.Parse("CAT-BBBBBB"), "Cool Down"),
            ]);

        // Act
        var cut = Render<Categories>();

        // Assert
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Warm Up", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Cool Down", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddCategory_ClickPlusCategory_AppendsRowInEditMode()
    {
        // Arrange
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([]);
        var created = new CategorySummary(CategoryId.Parse("CAT-CCCCCC"), "New Category");
        _apiClient
            .CreateCategoryAsync("New Category", CancellationToken.None)
            .Returns(new CreateCategorySucceeded(created));
        var cut = Render<Categories>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-category-empty']").Click());

        // Assert
        await _apiClient.Received(1).CreateCategoryAsync("New Category", CancellationToken.None);
        var input = cut.Find("[data-testid='name-input-CAT-CCCCCC']");
        Assert.True(input.HasAttribute("autofocus"));
    }

    [Fact]
    public async Task AddCategory_ServerRejectsDuplicateName_ShowsErrorAndAddsNoRow()
    {
        // Arrange
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([]);
        _apiClient
            .CreateCategoryAsync("New Category", CancellationToken.None)
            .Returns(new CreateCategoryFailed("A category named \"New Category\" already exists."));
        var cut = Render<Categories>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='add-category-empty']").Click());

        // Assert
        Assert.Contains(
            "A category named \"New Category\" already exists.",
            cut.Markup,
            StringComparison.Ordinal
        );
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public async Task RenameCategory_NameEditedAndSaveClicked_CallsRenameAndUpdatesDisplayedName()
    {
        // Arrange
        var category = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up");
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([category]);
        var renamed = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Cool Down");
        _apiClient
            .RenameCategoryAsync(
                CategoryId.Parse("CAT-AAAAAA"),
                "Cool Down",
                CancellationToken.None
            )
            .Returns(new RenameCategorySucceeded(renamed));
        var cut = Render<Categories>();
        var input = cut.Find("[data-testid='name-input-CAT-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Cool Down"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-CAT-AAAAAA']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .RenameCategoryAsync(
                CategoryId.Parse("CAT-AAAAAA"),
                "Cool Down",
                CancellationToken.None
            );
        Assert.Contains("Cool Down", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-CAT-AAAAAA']"));
    }

    [Fact]
    public async Task RenameCategory_NameFailsLengthBound_ShowsErrorAndDoesNotCallApi()
    {
        // Arrange
        var category = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up");
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([category]);
        var cut = Render<Categories>();
        var input = cut.Find("[data-testid='name-input-CAT-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Ab"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-CAT-AAAAAA']").Click());

        // Assert
        await _apiClient
            .DidNotReceive()
            .RenameCategoryAsync(
                Arg.Any<CategoryId>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            );
        Assert.NotEmpty(cut.FindAll("[data-testid='name-error-CAT-AAAAAA']"));
    }

    [Fact]
    public async Task RenameCategory_ServerRejects_ShowsErrorWithoutThrowing()
    {
        // Arrange
        var category = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up");
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([category]);
        _apiClient
            .RenameCategoryAsync(
                CategoryId.Parse("CAT-AAAAAA"),
                "Cool Down",
                CancellationToken.None
            )
            .Returns(new RenameCategoryFailed("name must be between 4 and 100 characters"));
        var cut = Render<Categories>();
        var input = cut.Find("[data-testid='name-input-CAT-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Cool Down"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-CAT-AAAAAA']").Click());

        // Assert
        Assert.Contains(
            "name must be between 4 and 100 characters",
            cut.Markup,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void SaveButton_NoUnsavedChanges_IsNotShown()
    {
        // Arrange
        var category = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up");
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([category]);

        // Act
        var cut = Render<Categories>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='save-CAT-AAAAAA']"));
    }

    [Fact]
    public async Task SaveButton_NameEdited_BecomesVisible()
    {
        // Arrange
        var category = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up");
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([category]);
        var cut = Render<Categories>();
        var input = cut.Find("[data-testid='name-input-CAT-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Cool Down"));

        // Assert
        Assert.Single(cut.FindAll("[data-testid='save-CAT-AAAAAA']"));
    }

    [Fact]
    public void RevertButton_NoUnsavedChanges_IsNotShown()
    {
        // Arrange
        var category = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up");
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([category]);

        // Act
        var cut = Render<Categories>();

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='revert-CAT-AAAAAA']"));
    }

    [Fact]
    public async Task RevertButton_NameEdited_BecomesVisible()
    {
        // Arrange
        var category = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up");
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([category]);
        var cut = Render<Categories>();
        var input = cut.Find("[data-testid='name-input-CAT-AAAAAA']");

        // Act
        await cut.InvokeAsync(() => input.Input("Cool Down"));

        // Assert
        Assert.Single(cut.FindAll("[data-testid='revert-CAT-AAAAAA']"));
    }

    [Fact]
    public async Task RevertCategory_ClickRevert_RestoresOriginalNameAndHidesButtons()
    {
        // Arrange
        var category = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up");
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([category]);
        var cut = Render<Categories>();
        var input = cut.Find("[data-testid='name-input-CAT-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Cool Down"));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='revert-CAT-AAAAAA']").Click());

        // Assert
        Assert.Contains("value=\"Warm Up\"", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='save-CAT-AAAAAA']"));
        Assert.Empty(cut.FindAll("[data-testid='revert-CAT-AAAAAA']"));
    }

    [Fact]
    public async Task RevertCategory_NameShowingValidationError_ClearsError()
    {
        // Arrange
        var category = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up");
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([category]);
        var cut = Render<Categories>();
        var input = cut.Find("[data-testid='name-input-CAT-AAAAAA']");
        await cut.InvokeAsync(() => input.Input("Ab"));
        await cut.InvokeAsync(() => cut.Find("[data-testid='save-CAT-AAAAAA']").Click());
        Assert.NotEmpty(cut.FindAll("[data-testid='name-error-CAT-AAAAAA']"));

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='revert-CAT-AAAAAA']").Click());

        // Assert
        Assert.Empty(cut.FindAll("[data-testid='name-error-CAT-AAAAAA']"));
    }

    [Fact]
    public async Task DeleteCategory_ClickDelete_CallsDeleteAndRemovesRow()
    {
        // Arrange
        var category = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up");
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([category]);
        _apiClient
            .DeleteCategoryAsync(CategoryId.Parse("CAT-AAAAAA"), CancellationToken.None)
            .Returns(new DeleteCategorySucceeded());
        var cut = Render<Categories>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-CAT-AAAAAA']").Click());

        // Assert
        await _apiClient
            .Received(1)
            .DeleteCategoryAsync(CategoryId.Parse("CAT-AAAAAA"), CancellationToken.None);
        Assert.Empty(cut.FindAll("tbody tr"));
    }

    [Fact]
    public async Task DeleteCategory_ServerRejects_ShowsErrorAndKeepsRowWithoutThrowing()
    {
        // Arrange
        var category = new CategorySummary(CategoryId.Parse("CAT-AAAAAA"), "Warm Up");
        _apiClient.GetCategoriesAsync(CancellationToken.None).Returns([category]);
        _apiClient
            .DeleteCategoryAsync(CategoryId.Parse("CAT-AAAAAA"), CancellationToken.None)
            .Returns(new DeleteCategoryFailed("Request failed with status 500."));
        var cut = Render<Categories>();

        // Act
        await cut.InvokeAsync(() => cut.Find("[data-testid='delete-CAT-AAAAAA']").Click());

        // Assert
        Assert.Contains("Request failed with status 500.", cut.Markup, StringComparison.Ordinal);
        Assert.Single(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void OnInitialized_LoadFails_ShowsErrorWithoutThrowing()
    {
        // Arrange
        _apiClient
            .GetCategoriesAsync(CancellationToken.None)
            .Returns<Task<IReadOnlyList<CategorySummary>>>(_ =>
                throw new HttpRequestException("simulated failure")
            );

        // Act
        var cut = Render<Categories>();

        // Assert
        Assert.NotEmpty(cut.FindAll("[data-testid='load-categories-error']"));
        Assert.Empty(cut.FindAll("tbody tr"));
    }
}
