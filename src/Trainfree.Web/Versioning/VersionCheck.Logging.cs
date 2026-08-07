using Microsoft.Extensions.Logging;

namespace Trainfree.Web.Versioning;

internal sealed partial class VersionCheck
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Could not reach the version endpoint; staleness is unknown. {Reason}"
    )]
    private partial void LogVersionUnreachable(string reason);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Could not read the deployed version; staleness is unknown. {Reason}"
    )]
    private partial void LogVersionUnreadable(string reason);
}
