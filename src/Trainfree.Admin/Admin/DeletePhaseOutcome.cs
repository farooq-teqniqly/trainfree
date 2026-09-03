namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to delete a phase.</summary>
internal abstract record DeletePhaseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected DeletePhaseOutcome() { }
}

/// <summary>
/// The delete succeeded, or the phase was already gone (a 404 is treated as success --
/// the caller's desired end state, "this phase no longer exists," already holds).
/// </summary>
internal sealed record DeletePhaseSucceeded : DeletePhaseOutcome;

/// <summary>The delete failed for a reason other than the phase already being gone.</summary>
internal sealed record DeletePhaseFailed(string Error) : DeletePhaseOutcome;
