namespace Trainfree.Admin.Admin;

/// <summary>The upload was rejected; carries the server-supplied error message.</summary>
internal sealed record UploadExerciseImageFailed(string Error) : UploadExerciseImageOutcome;
