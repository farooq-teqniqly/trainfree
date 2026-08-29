using Microsoft.Extensions.Logging;
using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Pages.Admin;

public partial class Programs
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load programs from the API.")]
    private static partial void LogLoadProgramsFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to load sessions for program {ProgramId}."
    )]
    private static partial void LogLoadSessionsForProgramFailed(
        ILogger logger,
        ProgramId programId,
        Exception exception
    );
}
