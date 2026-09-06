namespace Trainfree.Admin.Pages;

public partial class Exercises
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load exercises from the API.")]
    private static partial void LogLoadExercisesFailed(ILogger logger, Exception exception);
}
