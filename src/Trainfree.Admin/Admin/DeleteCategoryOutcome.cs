namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to delete a category.</summary>
internal abstract record DeleteCategoryOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected DeleteCategoryOutcome() { }
}

/// <summary>
/// The delete succeeded, or the category was already gone (a 404 is treated as success --
/// the caller's desired end state, "this category no longer exists," already holds).
/// </summary>
internal sealed record DeleteCategorySucceeded : DeleteCategoryOutcome;

/// <summary>The delete failed for a reason other than the category already being gone.</summary>
internal sealed record DeleteCategoryFailed(string Error) : DeleteCategoryOutcome;
