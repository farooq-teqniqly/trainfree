using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>Client for the Worker's <c>/api/programs/:programId/sessions</c> endpoints.</summary>
internal interface ISessionsApiClient
{
    /// <summary>Retrieves all of a program's sessions in creation order.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(
        ProgramId programId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates a new session under a program.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="name">The session name, 4-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="CreateSessionSucceeded"/> on success, or a <see cref="CreateSessionFailed"/>
    /// carrying an error message when the server rejects the request (e.g. a duplicate name
    /// within the program) or a transport/parse exception is caught.
    /// </returns>
    Task<CreateSessionOutcome> CreateSessionAsync(
        ProgramId programId,
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Renames an existing session.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="id">The session's identifier.</param>
    /// <param name="name">The new name, 4-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="RenameSessionSucceeded"/> on success, or a <see cref="RenameSessionFailed"/>
    /// carrying an error message when the server rejects the request or a transport/parse
    /// exception is caught.
    /// </returns>
    Task<RenameSessionOutcome> RenameSessionAsync(
        ProgramId programId,
        SessionId id,
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Deletes a session.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="id">The session's identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="DeleteSessionSucceeded"/> when the session is deleted or was already
    /// gone, or a <see cref="DeleteSessionFailed"/> carrying an error message for any other
    /// non-success response or a transport/parse exception caught during the request.
    /// </returns>
    Task<DeleteSessionOutcome> DeleteSessionAsync(
        ProgramId programId,
        SessionId id,
        CancellationToken cancellationToken = default
    );
}
