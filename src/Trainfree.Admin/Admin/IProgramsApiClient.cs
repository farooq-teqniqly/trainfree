using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>Client for the Worker's <c>/api/programs</c> endpoints.</summary>
internal interface IProgramsApiClient
{
    /// <summary>Retrieves all programs in creation order.</summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<IReadOnlyList<ProgramSummary>> GetProgramsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates a new program with the given name.</summary>
    /// <param name="name">The program name, 4-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="CreateProgramSucceeded"/> on success, or a <see cref="CreateProgramFailed"/>
    /// carrying the server's error message when the server rejects the request (e.g. a
    /// duplicate name).
    /// </returns>
    Task<CreateProgramOutcome> CreateProgramAsync(
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Renames an existing program.</summary>
    /// <param name="id">The program's identifier.</param>
    /// <param name="name">The new name, 4-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="RenameProgramSucceeded"/> on success, or a <see cref="RenameProgramFailed"/>
    /// carrying the server's error message when the server rejects the request.
    /// </returns>
    Task<RenameProgramOutcome> RenameProgramAsync(
        ProgramId id,
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Deletes a program.</summary>
    /// <param name="id">The program's identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="DeleteProgramSucceeded"/> when the program is deleted or was already
    /// gone, or a <see cref="DeleteProgramFailed"/> carrying the server's error message for
    /// any other non-success response.
    /// </returns>
    Task<DeleteProgramOutcome> DeleteProgramAsync(
        ProgramId id,
        CancellationToken cancellationToken = default
    );
}
