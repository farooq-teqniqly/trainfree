namespace Trainfree.Admin.Pages;

public partial class Programs
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to load the program tree from the API."
    )]
    private static partial void LogLoadProgramTreeFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load the phase library.")]
    private static partial void LogLoadPhaseLibraryFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load the exercise library.")]
    private static partial void LogLoadExerciseLibraryFailed(ILogger logger, Exception exception);
}
