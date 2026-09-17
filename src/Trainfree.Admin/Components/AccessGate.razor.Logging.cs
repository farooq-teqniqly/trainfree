using Microsoft.Extensions.Logging;

namespace Trainfree.Admin.Components;

public sealed partial class AccessGate
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Access check was in flight when the component was disposed; the request was canceled."
    )]
    private static partial void LogDisposedWhileCheckInFlight(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "IAccessCheck threw an exception its contract says it shouldn't; treating as AccessCheckFailed."
    )]
    private static partial void LogAccessCheckThrewUnexpectedly(
        ILogger logger,
        Exception exception
    );
}
