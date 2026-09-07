using Trainfree.Domain.Ids;

namespace Trainfree.Domain.ProgramExercises;

/// <summary>
/// Common surface shared by <see cref="RepsProgramExercise"/> and
/// <see cref="TimedProgramExercise"/> -- the fields every program exercise carries
/// regardless of whether its count is a rep count or a duration.
/// </summary>
public interface IProgramExercise
{
    /// <summary>The program exercise's surrogate identifier.</summary>
    ProgramExerciseId Id { get; }

    /// <summary>The session phase this program exercise belongs to.</summary>
    SessionPhaseId SessionPhaseId { get; }

    /// <summary>The canonical exercise this program exercise references.</summary>
    ExerciseId ExerciseId { get; }

    /// <summary>The prescribed load, in pounds. Zero when no load is prescribed.</summary>
    decimal Weight { get; }

    /// <summary>The number of sets prescribed. Always strictly positive.</summary>
    int Sets { get; }

    /// <summary>The rest between sets, in seconds. Always strictly positive.</summary>
    int RestSeconds { get; }

    /// <summary>Which side of the body the exercise is prescribed for.</summary>
    ProgramExerciseSide Side { get; }
}
