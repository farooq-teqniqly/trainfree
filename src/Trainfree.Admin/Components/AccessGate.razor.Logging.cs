using Microsoft.Extensions.Logging;

namespace Trainfree.Admin.Components;

public sealed partial class AccessGate
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Access check was in flight when the component was disposed; the request was canceled."
    )]
    private static partial void LogDisposedWhileCheckInFlight(ILogger logger);
}
