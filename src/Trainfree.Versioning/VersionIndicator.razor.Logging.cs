using Microsoft.Extensions.Logging;

namespace Trainfree.Versioning;

public sealed partial class VersionIndicator
{
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Version check was in flight when the component was disposed; the request was canceled."
    )]
    private static partial void LogDisposedWhileCheckInFlight(ILogger logger);
}
