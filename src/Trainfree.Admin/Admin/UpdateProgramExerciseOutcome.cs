using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to update a program exercise's prescription.</summary>
internal abstract record UpdateProgramExerciseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected UpdateProgramExerciseOutcome() { }
}

/// <summary>The update succeeded; carries the updated program exercise.</summary>
internal sealed record UpdateProgramExerciseSucceeded(IProgramExercise ProgramExercise)
    : UpdateProgramExerciseOutcome;

/// <summary>The update was rejected; carries the server-supplied error message.</summary>
internal sealed record UpdateProgramExerciseFailed(string Error) : UpdateProgramExerciseOutcome;
