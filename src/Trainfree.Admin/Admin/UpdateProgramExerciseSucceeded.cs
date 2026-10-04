using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Admin;

/// <summary>The update succeeded; carries the updated program exercise.</summary>
internal sealed record UpdateProgramExerciseSucceeded(IProgramExercise ProgramExercise)
    : UpdateProgramExerciseOutcome;
