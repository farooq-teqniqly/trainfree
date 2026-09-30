namespace Trainfree.Admin.Pages;

public partial class Exercises
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load exercises from the API.")]
    private static partial void LogLoadExercisesFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to read or resize the picked image."
    )]
    private static partial void LogImageProcessingFailed(ILogger logger, Exception exception);
}
