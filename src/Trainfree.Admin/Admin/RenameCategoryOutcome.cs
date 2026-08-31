namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to rename a category.</summary>
internal abstract record RenameCategoryOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected RenameCategoryOutcome() { }
}

/// <summary>The rename succeeded; carries the updated category.</summary>
internal sealed record RenameCategorySucceeded(CategorySummary Category) : RenameCategoryOutcome;

/// <summary>The rename was rejected; carries the server-supplied error message.</summary>
internal sealed record RenameCategoryFailed(string Error) : RenameCategoryOutcome;
