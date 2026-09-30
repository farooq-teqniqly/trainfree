namespace Trainfree.Admin.Admin;

/// <summary>
/// What an exercise row's image slot holds: nothing, a stored image, or a processed file
/// staged for upload. Each state carries only the data meaningful in that state.
/// </summary>
internal abstract record ExerciseImage
{
    // Closes the hierarchy to the three states declared in this folder.
    private protected ExerciseImage() { }
}
