using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>An exercise as displayed in the admin UI.</summary>
/// <param name="Id">The exercise's identifier.</param>
/// <param name="Name">The exercise's name.</param>
/// <param name="ImageUrl">
/// The same-origin relative path of the exercise's image, or <see langword="null"/> when it
/// has none. This mirrors the wire shape; the UI maps it to an <see cref="ExerciseImage"/>.
/// </param>
public sealed record ExerciseSummary(ExerciseId Id, string Name, string? ImageUrl = null);
