using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Components;

/// <summary>
/// Editable-row view model for a single session, tracking the working name against the
/// last-saved name so <see cref="IsDirty" /> can gate Save/Revert.
/// </summary>
public sealed class SessionRow
{
    /// <summary>
    /// Initializes the row from a saved session.
    /// </summary>
    /// <param name="id">The identifier of the session.</param>
    /// <param name="programId">The identifier of the program the session belongs to.</param>
    /// <param name="name">The saved name of the session.</param>
    public SessionRow(SessionId id, ProgramId programId, string name)
    {
        Id = id;
        ProgramId = programId;
        Name = name;
        SavedName = name;
    }

    /// <summary>The identifier of the session.</summary>
    public SessionId Id { get; }

    /// <summary>The identifier of the program the session belongs to.</summary>
    public ProgramId ProgramId { get; }

    /// <summary>The working name of the session.</summary>
    public string Name { get; set; }

    /// <summary>The last-saved value of <see cref="Name" />.</summary>
    public string SavedName { get; set; }

    /// <summary>Whether <see cref="Name" /> differs from <see cref="SavedName" />.</summary>
    public bool IsDirty => !string.Equals(Name, SavedName, StringComparison.Ordinal);

    /// <summary>Whether a save is currently in flight for this row.</summary>
    public bool IsSaving { get; set; }

    /// <summary>The error message from the row's last failed save, if any.</summary>
    public string? Error { get; set; }

    /// <summary>The error message from the row's last failed phases load, if any.</summary>
    public string? PhasesLoadError { get; set; }

    /// <summary>The session's phases.</summary>
    public List<SessionPhaseRow> Phases { get; } = [];
}
