using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Admin;

/// <summary>
/// A partial update to a program exercise's prescription. Every property is optional --
/// only the properties set are sent to the Worker, which updates just those fields.
/// </summary>
internal sealed record ProgramExerciseUpdate(
    int? Reps = null,
    int? DurationSeconds = null,
    decimal? Weight = null,
    int? Sets = null,
    int? RestSeconds = null,
    ProgramExerciseSide? Side = null
);
