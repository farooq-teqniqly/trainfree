namespace Trainfree.Admin.Admin;

/// <summary>The delete failed for a reason other than the program exercise already being gone.</summary>
internal sealed record DeleteProgramExerciseFailed(string Error) : DeleteProgramExerciseOutcome;
