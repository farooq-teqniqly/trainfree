using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Admin;

/// <summary>A session phase with its program exercises nested underneath.</summary>
internal sealed record SessionPhaseTreeItem(
    SessionPhaseSummary SessionPhase,
    IReadOnlyList<IProgramExercise> Exercises
);
