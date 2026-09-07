using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>
/// Client for the Worker's <c>/api/programs/:programId/sessions/:sessionId/phases</c>
/// endpoints.
/// </summary>
internal interface ISessionPhasesApiClient
{
    /// <summary>Retrieves all of a session's phases in creation order.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="sessionId">The owning session's identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<IReadOnlyList<SessionPhaseSummary>> GetSessionPhasesAsync(
        ProgramId programId,
        SessionId sessionId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Adds a phase to a session.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="sessionId">The owning session's identifier.</param>
    /// <param name="phaseId">The identifier of the phase to add.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="CreateSessionPhaseSucceeded"/> on success, or a
    /// <see cref="CreateSessionPhaseFailed"/> carrying an error message when the server
    /// rejects the request (e.g. an unknown <paramref name="phaseId"/>) or a
    /// transport/parse exception is caught.
    /// </returns>
    Task<CreateSessionPhaseOutcome> CreateSessionPhaseAsync(
        ProgramId programId,
        SessionId sessionId,
        PhaseId phaseId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Deletes a session phase.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="sessionId">The owning session's identifier.</param>
    /// <param name="id">The session phase's identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="DeleteSessionPhaseSucceeded"/> when the session phase is deleted or
    /// was already gone, or a <see cref="DeleteSessionPhaseFailed"/> carrying an error
    /// message for any other non-success response or a transport/parse exception caught
    /// during the request.
    /// </returns>
    Task<DeleteSessionPhaseOutcome> DeleteSessionPhaseAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId id,
        CancellationToken cancellationToken = default
    );
}
