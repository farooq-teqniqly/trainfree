using Trainfree.Web.Ids;

namespace Trainfree.Web.Admin;

/// <summary>Client for the Worker's <c>/api/programs</c> endpoints.</summary>
internal interface IProgramsApiClient
{
    /// <summary>Retrieves all programs in creation order.</summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<IReadOnlyList<ProgramSummary>> GetProgramsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates a new program with the given name.</summary>
    /// <param name="name">The program name, 5-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<ProgramSummary> CreateProgramAsync(
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Renames an existing program.</summary>
    /// <param name="id">The program's identifier.</param>
    /// <param name="name">The new name, 5-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<ProgramSummary> RenameProgramAsync(
        ProgramId id,
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Deletes a program.</summary>
    /// <param name="id">The program's identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task DeleteProgramAsync(ProgramId id, CancellationToken cancellationToken = default);
}
