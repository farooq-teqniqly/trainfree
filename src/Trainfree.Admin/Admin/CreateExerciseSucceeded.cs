namespace Trainfree.Admin.Admin;

/// <summary>The create succeeded; carries the created exercise.</summary>
internal sealed record CreateExerciseSucceeded(ExerciseSummary Exercise) : CreateExerciseOutcome;
