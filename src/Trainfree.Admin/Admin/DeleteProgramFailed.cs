namespace Trainfree.Admin.Admin;

/// <summary>The delete failed for a reason other than the program already being gone.</summary>
internal sealed record DeleteProgramFailed(string Error) : DeleteProgramOutcome;
