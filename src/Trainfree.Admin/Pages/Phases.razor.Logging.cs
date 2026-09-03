namespace Trainfree.Admin.Pages;

public partial class Phases
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load phases from the API.")]
    private static partial void LogLoadPhasesFailed(ILogger logger, Exception exception);
}
