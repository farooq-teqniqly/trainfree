namespace Trainfree.Admin.Admin;

/// <summary>The update was rejected; carries the server-supplied error message.</summary>
internal sealed record UpdateProgramExerciseFailed(string Error) : UpdateProgramExerciseOutcome;
