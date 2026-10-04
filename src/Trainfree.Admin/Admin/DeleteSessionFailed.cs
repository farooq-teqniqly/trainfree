namespace Trainfree.Admin.Admin;

/// <summary>The delete failed for a reason other than the session already being gone.</summary>
internal sealed record DeleteSessionFailed(string Error) : DeleteSessionOutcome;
