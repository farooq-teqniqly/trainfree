namespace Trainfree.Admin.Admin;

/// <summary>The exercise has a stored image served from the Worker.</summary>
internal sealed record HasImage : ExerciseImage
{
    /// <summary>Gets the same-origin relative path that returns the image bytes.</summary>
    public string Url { get; }

    /// <summary>Initializes a new instance of <see cref="HasImage"/>.</summary>
    /// <param name="url">The same-origin relative path that returns the image bytes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="url"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is empty or whitespace.</exception>
    public HasImage(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        Url = url;
    }
}
