namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to create a category.</summary>
internal abstract record CreateCategoryOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected CreateCategoryOutcome() { }
}

/// <summary>The create succeeded; carries the created category.</summary>
internal sealed record CreateCategorySucceeded(CategorySummary Category) : CreateCategoryOutcome;

/// <summary>The create was rejected; carries the server-supplied error message.</summary>
internal sealed record CreateCategoryFailed(string Error) : CreateCategoryOutcome;
