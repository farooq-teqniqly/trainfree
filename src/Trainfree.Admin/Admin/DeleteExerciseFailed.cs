namespace Trainfree.Admin.Admin;

/// <summary>The delete failed for a reason other than the exercise already being gone.</summary>
internal sealed record DeleteExerciseFailed(string Error) : DeleteExerciseOutcome;
