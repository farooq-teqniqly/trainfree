using Microsoft.Extensions.Logging;

namespace Trainfree.Web.Admin;

internal sealed partial class ProgramsApiClient
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "A failure response declared JSON but could not be read; "
            + "reporting the status code instead."
    )]
    private static partial void LogErrorBodyUnreadable(ILogger logger, Exception exception);
}
