using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Components;

/// <summary>
/// Editable-row view model for a single program, tracking the working name against the
/// last-saved name so <see cref="IsDirty" /> can gate Save/Revert.
/// </summary>
public sealed class ProgramRow
{
    /// <summary>
    /// Initializes the row from a saved program.
    /// </summary>
    /// <param name="id">The identifier of the program.</param>
    /// <param name="name">The saved name of the program.</param>
    public ProgramRow(ProgramId id, string name)
    {
        Id = id;
        Name = name;
        SavedName = name;
    }

    /// <summary>The identifier of the program.</summary>
    public ProgramId Id { get; }

    /// <summary>The working name of the program.</summary>
    public string Name { get; set; }

    /// <summary>The last-saved value of <see cref="Name" />.</summary>
    public string SavedName { get; set; }

    /// <summary>Whether <see cref="Name" /> differs from <see cref="SavedName" />.</summary>
    public bool IsDirty => !string.Equals(Name, SavedName, StringComparison.Ordinal);

    /// <summary>Whether a save is currently in flight for this row.</summary>
    public bool IsSaving { get; set; }

    /// <summary>The error message from the row's last failed save, if any.</summary>
    public string? Error { get; set; }

    /// <summary>The error message from the row's last failed sessions load, if any.</summary>
    public string? SessionsLoadError { get; set; }

    /// <summary>The program's sessions.</summary>
    public List<SessionRow> Sessions { get; } = [];
}
