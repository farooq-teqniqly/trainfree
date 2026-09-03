using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>Client for the Worker's <c>/api/phases</c> endpoints.</summary>
internal interface IPhasesApiClient
{
    /// <summary>Retrieves all phases in creation order.</summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<IReadOnlyList<PhaseSummary>> GetPhasesAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a new phase with the given name.</summary>
    /// <param name="name">The phase name, 4-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="CreatePhaseSucceeded"/> on success, or a <see cref="CreatePhaseFailed"/>
    /// carrying the server's error message when the server rejects the request (e.g. a
    /// duplicate name).
    /// </returns>
    Task<CreatePhaseOutcome> CreatePhaseAsync(
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Renames an existing phase.</summary>
    /// <param name="id">The phase's identifier.</param>
    /// <param name="name">The new name, 4-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="RenamePhaseSucceeded"/> on success, or a <see cref="RenamePhaseFailed"/>
    /// carrying the server's error message when the server rejects the request.
    /// </returns>
    Task<RenamePhaseOutcome> RenamePhaseAsync(
        PhaseId id,
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Deletes a phase.</summary>
    /// <param name="id">The phase's identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="DeletePhaseSucceeded"/> when the phase is deleted or was already
    /// gone, or a <see cref="DeletePhaseFailed"/> carrying the server's error message for
    /// any other non-success response.
    /// </returns>
    Task<DeletePhaseOutcome> DeletePhaseAsync(
        PhaseId id,
        CancellationToken cancellationToken = default
    );
}
