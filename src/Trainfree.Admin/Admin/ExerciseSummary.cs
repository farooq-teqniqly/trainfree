using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>An exercise as displayed in the admin UI.</summary>
public sealed record ExerciseSummary(ExerciseId Id, string Name);
