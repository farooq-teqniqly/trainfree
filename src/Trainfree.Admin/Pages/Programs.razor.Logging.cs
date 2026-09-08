using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Pages;

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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load the phase library.")]
    private static partial void LogLoadPhaseLibraryFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to load phases for session {SessionId}."
    )]
    private static partial void LogLoadPhasesForSessionFailed(
        ILogger logger,
        SessionId sessionId,
        Exception exception
    );

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load the exercise library.")]
    private static partial void LogLoadExerciseLibraryFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to load program exercises for session phase {SessionPhaseId}."
    )]
    private static partial void LogLoadProgramExercisesForSessionPhaseFailed(
        ILogger logger,
        SessionPhaseId sessionPhaseId,
        Exception exception
    );
}
