namespace Trainfree.Admin.Admin;

/// <summary>
/// The image was deleted, or was already gone (a 404 is treated as success -- the caller's
/// desired end state, "this exercise has no image," already holds).
/// </summary>
internal sealed record DeleteExerciseImageSucceeded : DeleteExerciseImageOutcome;
