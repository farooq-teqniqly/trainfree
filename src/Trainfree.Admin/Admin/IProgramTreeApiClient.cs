namespace Trainfree.Admin.Admin;

/// <summary>Client for the Worker's <c>/api/programs-tree</c> endpoint.</summary>
internal interface IProgramTreeApiClient
{
    /// <summary>
    /// Retrieves every program with its sessions, session phases, and program exercises
    /// nested underneath, in a single request.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<IReadOnlyList<ProgramTreeItem>> GetProgramTreeAsync(
        CancellationToken cancellationToken = default
    );
}
