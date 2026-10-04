namespace Trainfree.Admin.Admin;

/// <summary>
/// The delete failed for a reason other than the image already being gone; carries the
/// server-supplied error message.
/// </summary>
internal sealed record DeleteExerciseImageFailed(string Error) : DeleteExerciseImageOutcome;
