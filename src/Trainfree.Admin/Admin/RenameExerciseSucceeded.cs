namespace Trainfree.Admin.Admin;

/// <summary>The rename succeeded; carries the updated exercise.</summary>
internal sealed record RenameExerciseSucceeded(ExerciseSummary Exercise) : RenameExerciseOutcome;
