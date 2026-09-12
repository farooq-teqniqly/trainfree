using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Components;

/// <summary>
/// Read-only-name view model for a phase attached to a session, tracking the state of its
/// add-exercise form and the program exercises loaded under it.
/// </summary>
public sealed class SessionPhaseRow
{
    /// <summary>
    /// Initializes the row from a saved session phase.
    /// </summary>
    /// <param name="id">The identifier of the session phase.</param>
    /// <param name="phaseId">The identifier of the phase.</param>
    /// <param name="displayName">The display name of the phase.</param>
    /// <exception cref="ArgumentException"><paramref name="displayName" /> is null, empty, or whitespace.</exception>
    public SessionPhaseRow(SessionPhaseId id, PhaseId phaseId, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Id = id;
        PhaseId = phaseId;
        DisplayName = displayName;
    }

    /// <summary>The identifier of the session phase.</summary>
    public SessionPhaseId Id { get; }

    /// <summary>The identifier of the phase.</summary>
    public PhaseId PhaseId { get; }

    /// <summary>The display name of the phase.</summary>
    public string DisplayName { get; }

    /// <summary>The error message from the row's last failed operation, if any.</summary>
    public string? Error { get; set; }

    /// <summary>The error message from the row's last failed add-exercise attempt, if any.</summary>
    public string? AddExerciseError { get; set; }

    /// <summary>Whether the add-exercise form is currently open for this row.</summary>
    public bool IsAddingExercise { get; set; }

    /// <summary>A generation counter bumped each time the add-exercise form is reopened.</summary>
    public int AddExerciseFormGeneration { get; set; }

    /// <summary>The program exercises prescribed under this phase.</summary>
    public IReadOnlyList<ProgramExerciseRow> ProgramExercises => _programExercises;

    private readonly List<ProgramExerciseRow> _programExercises = [];

    /// <summary>Adds a program exercise to <see cref="ProgramExercises" />.</summary>
    /// <param name="programExercise">The program exercise row to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="programExercise" /> is <see langword="null" />.</exception>
    internal void AddProgramExercise(ProgramExerciseRow programExercise)
    {
        ArgumentNullException.ThrowIfNull(programExercise);

        _programExercises.Add(programExercise);
    }

    /// <summary>Adds a range of program exercises to <see cref="ProgramExercises" />.</summary>
    /// <param name="programExercises">The program exercise rows to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="programExercises" /> is <see langword="null" />.</exception>
    internal void AddProgramExercises(IEnumerable<ProgramExerciseRow> programExercises)
    {
        ArgumentNullException.ThrowIfNull(programExercises);

        _programExercises.AddRange(programExercises);
    }

    /// <summary>Removes a program exercise from <see cref="ProgramExercises" />.</summary>
    /// <param name="programExercise">The program exercise row to remove.</param>
    /// <exception cref="ArgumentNullException"><paramref name="programExercise" /> is <see langword="null" />.</exception>
    internal void RemoveProgramExercise(ProgramExerciseRow programExercise)
    {
        ArgumentNullException.ThrowIfNull(programExercise);

        _programExercises.Remove(programExercise);
    }
}
