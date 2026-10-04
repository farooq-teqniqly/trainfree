namespace Trainfree.Admin.Admin;

/// <summary>
/// The delete succeeded, or the phase was already gone (a 404 is treated as success --
/// the caller's desired end state, "this phase no longer exists," already holds).
/// </summary>
internal sealed record DeletePhaseSucceeded : DeletePhaseOutcome;
