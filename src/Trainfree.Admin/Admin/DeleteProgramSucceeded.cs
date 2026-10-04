namespace Trainfree.Admin.Admin;

/// <summary>
/// The delete succeeded, or the program was already gone (a 404 is treated as success --
/// the caller's desired end state, "this program no longer exists," already holds).
/// </summary>
internal sealed record DeleteProgramSucceeded : DeleteProgramOutcome;
