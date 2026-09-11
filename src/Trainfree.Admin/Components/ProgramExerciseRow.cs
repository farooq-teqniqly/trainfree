using Trainfree.Domain.Ids;
using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Components;

/// <summary>
/// Editable-row view model for a single program exercise, tracking working values against the
/// last-saved values so <see cref="IsDirty" /> can gate Save/Revert.
/// </summary>
public sealed class ProgramExerciseRow
{
    /// <summary>
    /// Initializes the row from a saved program exercise.
    /// </summary>
    /// <param name="programExercise">The saved program exercise to seed the row from.</param>
    /// <param name="exerciseName">The display name of the exercise being prescribed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="programExercise" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="exerciseName" /> is null, empty, or whitespace.</exception>
    public ProgramExerciseRow(IProgramExercise programExercise, string exerciseName)
    {
        ArgumentNullException.ThrowIfNull(programExercise);
        ArgumentException.ThrowIfNullOrWhiteSpace(exerciseName);

        Id = programExercise.Id;
        ExerciseId = programExercise.ExerciseId;
        ExerciseName = exerciseName;
        IsTimed = programExercise is TimedProgramExercise;
        ApplySaved(programExercise);
    }

    /// <summary>The identifier of the program exercise.</summary>
    public ProgramExerciseId Id { get; }

    /// <summary>The identifier of the exercise being prescribed.</summary>
    public ExerciseId ExerciseId { get; }

    /// <summary>The display name of the exercise being prescribed.</summary>
    public string ExerciseName { get; }

    /// <summary>Whether the exercise is prescribed by duration rather than reps.</summary>
    public bool IsTimed { get; }

    /// <summary>The working reps, or duration in seconds when <see cref="IsTimed" /> is <see langword="true" />.</summary>
    public int Count { get; set; }

    /// <summary>The last-saved value of <see cref="Count" />.</summary>
    public int SavedCount { get; set; }

    /// <summary>The working weight.</summary>
    public decimal Weight { get; set; }

    /// <summary>The last-saved value of <see cref="Weight" />.</summary>
    public decimal SavedWeight { get; set; }

    /// <summary>The raw text entered into the weight field.</summary>
    public string WeightText { get; set; } = "";

    /// <summary>Whether <see cref="WeightText" /> currently parses to a valid decimal.</summary>
    public bool WeightIsValid { get; set; } = true;

    /// <summary>The working number of sets.</summary>
    public int Sets { get; set; }

    /// <summary>The last-saved value of <see cref="Sets" />.</summary>
    public int SavedSets { get; set; }

    /// <summary>The working rest period, in seconds.</summary>
    public int RestSeconds { get; set; }

    /// <summary>The last-saved value of <see cref="RestSeconds" />.</summary>
    public int SavedRestSeconds { get; set; }

    /// <summary>The working side (left/right/both) the exercise is prescribed for.</summary>
    public ProgramExerciseSide Side { get; set; }

    /// <summary>The last-saved value of <see cref="Side" />.</summary>
    public ProgramExerciseSide SavedSide { get; set; }

    /// <summary>Whether a save is currently in flight for this row.</summary>
    public bool IsSaving { get; set; }

    /// <summary>The error message from the row's last failed save, if any.</summary>
    public string? Error { get; set; }

    /// <summary>Whether any working value differs from its last-saved counterpart.</summary>
    public bool IsDirty =>
        Count != SavedCount
        || Weight != SavedWeight
        || !WeightIsValid
        || Sets != SavedSets
        || RestSeconds != SavedRestSeconds
        || Side != SavedSide;

    /// <summary>
    /// Overwrites both the working and last-saved values from a freshly saved program exercise.
    /// </summary>
    /// <param name="programExercise">The program exercise to apply as the new saved state.</param>
    /// <exception cref="ArgumentNullException"><paramref name="programExercise" /> is <see langword="null" />.</exception>
    public void ApplySaved(IProgramExercise programExercise)
    {
        ArgumentNullException.ThrowIfNull(programExercise);

        Count = IsTimed
            ? ((TimedProgramExercise)programExercise).DurationSeconds
            : ((RepsProgramExercise)programExercise).Reps;
        SavedCount = Count;
        Weight = programExercise.Weight;
        SavedWeight = Weight;
        WeightText = DashIfZero(Weight);
        WeightIsValid = true;
        Sets = programExercise.Sets;
        SavedSets = Sets;
        RestSeconds = programExercise.RestSeconds;
        SavedRestSeconds = RestSeconds;
        Side = programExercise.Side;
        SavedSide = Side;
    }

    /// <summary>Sets the working <see cref="Count" /> by parsing a raw input value.</summary>
    /// <param name="value">The raw value from the input element.</param>
    public void SetCount(object? value) => Count = ParseInt(value);

    /// <summary>Sets the working <see cref="Sets" /> by parsing a raw input value.</summary>
    /// <param name="value">The raw value from the input element.</param>
    public void SetSets(object? value) => Sets = ParseInt(value);

    /// <summary>Sets the working <see cref="RestSeconds" /> by parsing a raw input value.</summary>
    /// <param name="value">The raw value from the input element.</param>
    public void SetRestSeconds(object? value) => RestSeconds = ParseInt(value);

    /// <summary>
    /// Sets the working weight from raw input text, updating <see cref="WeightIsValid" /> and,
    /// when valid, <see cref="Weight" />.
    /// </summary>
    /// <param name="text">The raw text entered into the weight field.</param>
    public void SetWeightText(string? text)
    {
        WeightText = text ?? "";
        WeightIsValid = decimal.TryParse(
            WeightText,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed
        );
        if (WeightIsValid)
        {
            Weight = parsed;
        }
    }

    /// <summary>Discards working values back to the last-saved values and clears the row's error.</summary>
    public void RevertToSaved()
    {
        Count = SavedCount;
        Weight = SavedWeight;
        WeightText = DashIfZero(SavedWeight);
        WeightIsValid = true;
        Sets = SavedSets;
        RestSeconds = SavedRestSeconds;
        Side = SavedSide;
        Error = null;
    }

    private static int ParseInt(object? value) =>
        int.TryParse(value as string, out var parsed) ? parsed : 0;

    private static string DashIfZero(decimal value) =>
        value == 0 ? "–" : value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
