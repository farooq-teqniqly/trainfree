namespace Trainfree.Admin.Admin;

/// <summary>The result of checking a picked image file's type and size before processing it.</summary>
internal abstract record ImageFileValidationOutcome
{
    // Closes the hierarchy to the two outcomes declared in this folder.
    private protected ImageFileValidationOutcome() { }
}
