using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>Client for the Worker's <c>/api/categories</c> endpoints.</summary>
internal interface ICategoriesApiClient
{
    /// <summary>Retrieves all categories in creation order.</summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<IReadOnlyList<CategorySummary>> GetCategoriesAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates a new category with the given name.</summary>
    /// <param name="name">The category name, 4-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="CreateCategorySucceeded"/> on success, or a <see cref="CreateCategoryFailed"/>
    /// carrying the server's error message when the server rejects the request (e.g. a
    /// duplicate name).
    /// </returns>
    Task<CreateCategoryOutcome> CreateCategoryAsync(
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Renames an existing category.</summary>
    /// <param name="id">The category's identifier.</param>
    /// <param name="name">The new name, 4-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="RenameCategorySucceeded"/> on success, or a <see cref="RenameCategoryFailed"/>
    /// carrying the server's error message when the server rejects the request.
    /// </returns>
    Task<RenameCategoryOutcome> RenameCategoryAsync(
        CategoryId id,
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Deletes a category.</summary>
    /// <param name="id">The category's identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="DeleteCategorySucceeded"/> when the category is deleted or was already
    /// gone, or a <see cref="DeleteCategoryFailed"/> carrying the server's error message for
    /// any other non-success response.
    /// </returns>
    Task<DeleteCategoryOutcome> DeleteCategoryAsync(
        CategoryId id,
        CancellationToken cancellationToken = default
    );
}
