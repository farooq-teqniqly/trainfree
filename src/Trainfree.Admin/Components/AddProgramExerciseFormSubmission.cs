using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Components;

/// <summary>
/// Carries the values entered into <see cref="AddExerciseForm" /> when the user submits it.
/// </summary>
/// <param name="ExerciseId">The exercise selected from the library.</param>
/// <param name="IsTimed">Whether the exercise is prescribed by duration rather than reps.</param>
/// <param name="Count">The reps, or the duration in seconds when <paramref name="IsTimed" /> is <see langword="true" />.</param>
/// <param name="Sets">The number of sets.</param>
/// <param name="RestSeconds">The rest between sets, in seconds.</param>
public sealed record AddProgramExerciseFormSubmission(
    ExerciseId ExerciseId,
    bool IsTimed,
    int Count,
    int Sets,
    int RestSeconds
);
