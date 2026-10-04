using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Admin;

/// <summary>The create succeeded; carries the created program exercise.</summary>
internal sealed record CreateProgramExerciseSucceeded(IProgramExercise ProgramExercise)
    : CreateProgramExerciseOutcome;
