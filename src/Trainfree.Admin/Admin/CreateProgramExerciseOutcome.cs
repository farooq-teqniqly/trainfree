using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Admin;

/// <summary>The result of attempting to create a program exercise.</summary>
internal abstract record CreateProgramExerciseOutcome
{
    // Closes the hierarchy to the two outcomes declared in this file.
    private protected CreateProgramExerciseOutcome() { }
}

/// <summary>The create succeeded; carries the created program exercise.</summary>
internal sealed record CreateProgramExerciseSucceeded(IProgramExercise ProgramExercise)
    : CreateProgramExerciseOutcome;

/// <summary>The create was rejected; carries the server-supplied error message.</summary>
internal sealed record CreateProgramExerciseFailed(string Error) : CreateProgramExerciseOutcome;
