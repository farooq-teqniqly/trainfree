namespace Trainfree.Admin.Admin;

/// <summary>
/// The delete succeeded, or the program exercise was already gone (a 404 is treated as
/// success -- the caller's desired end state, "this program exercise no longer exists,"
/// already holds).
/// </summary>
internal sealed record DeleteProgramExerciseSucceeded : DeleteProgramExerciseOutcome;
