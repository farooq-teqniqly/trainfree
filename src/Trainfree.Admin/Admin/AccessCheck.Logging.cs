using Microsoft.Extensions.Logging;

namespace Trainfree.Admin.Admin;

internal sealed partial class AccessCheck
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not reach the access-check endpoint. {Reason}"
    )]
    private partial void LogAccessCheckUnreachable(string reason);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Access-check endpoint returned server error {StatusCode}."
    )]
    private partial void LogAccessCheckServerError(int statusCode);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not read the access-check response. {Reason}"
    )]
    private partial void LogAccessCheckUnreadable(string reason);
}
