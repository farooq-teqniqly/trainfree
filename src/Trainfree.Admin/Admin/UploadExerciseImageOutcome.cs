namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to upload or replace an exercise's image.</summary>
internal abstract record UploadExerciseImageOutcome
{
    // Closes the hierarchy to the two outcomes declared in this folder.
    private protected UploadExerciseImageOutcome() { }
}
