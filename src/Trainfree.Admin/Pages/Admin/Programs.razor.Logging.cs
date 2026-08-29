using Microsoft.Extensions.Logging;

namespace Trainfree.Admin.Pages.Admin;

public partial class Programs
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load programs from the API.")]
    private static partial void LogLoadProgramsFailed(ILogger logger, Exception exception);
}
