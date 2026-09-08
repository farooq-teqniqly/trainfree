using Trainfree.Domain.Ids;
using Trainfree.Domain.ProgramExercises;

namespace Trainfree.Admin.Admin;

/// <summary>
/// Client for the Worker's
/// <c>/api/programs/:programId/sessions/:sessionId/phases/:sessionPhaseId/exercises</c>
/// endpoints.
/// </summary>
internal interface IProgramExercisesApiClient
{
    /// <summary>Retrieves all of a session phase's program exercises in creation order.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="sessionId">The owning session's identifier.</param>
    /// <param name="sessionPhaseId">The owning session phase's identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<IReadOnlyList<IProgramExercise>> GetProgramExercisesAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Adds a Reps-prescribed exercise to a session phase.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="sessionId">The owning session's identifier.</param>
    /// <param name="sessionPhaseId">The owning session phase's identifier.</param>
    /// <param name="exerciseId">The canonical exercise to reference.</param>
    /// <param name="reps">The number of reps prescribed per set. Must be strictly positive.</param>
    /// <param name="prescription">The number of sets and the rest between them.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="CreateProgramExerciseSucceeded"/> on success, or a
    /// <see cref="CreateProgramExerciseFailed"/> carrying an error message when the server
    /// rejects the request or a transport/parse exception is caught.
    /// </returns>
    Task<CreateProgramExerciseOutcome> CreateRepsProgramExerciseAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId,
        ExerciseId exerciseId,
        int reps,
        SetPrescription prescription,
        CancellationToken cancellationToken = default
    );

    /// <summary>Adds a Timed-prescribed exercise to a session phase.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="sessionId">The owning session's identifier.</param>
    /// <param name="sessionPhaseId">The owning session phase's identifier.</param>
    /// <param name="exerciseId">The canonical exercise to reference.</param>
    /// <param name="durationSeconds">The duration prescribed per set, in seconds. Must be strictly positive.</param>
    /// <param name="prescription">The number of sets and the rest between them.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="CreateProgramExerciseSucceeded"/> on success, or a
    /// <see cref="CreateProgramExerciseFailed"/> carrying an error message when the server
    /// rejects the request or a transport/parse exception is caught.
    /// </returns>
    Task<CreateProgramExerciseOutcome> CreateTimedProgramExerciseAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId,
        ExerciseId exerciseId,
        int durationSeconds,
        SetPrescription prescription,
        CancellationToken cancellationToken = default
    );

    /// <summary>Updates a program exercise's prescription.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="sessionId">The owning session's identifier.</param>
    /// <param name="sessionPhaseId">The owning session phase's identifier.</param>
    /// <param name="id">The program exercise's identifier.</param>
    /// <param name="update">The fields to update; unset properties are left unchanged.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// An <see cref="UpdateProgramExerciseSucceeded"/> on success, or an
    /// <see cref="UpdateProgramExerciseFailed"/> carrying an error message when the server
    /// rejects the request or a transport/parse exception is caught.
    /// </returns>
    Task<UpdateProgramExerciseOutcome> UpdateProgramExerciseAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId,
        ProgramExerciseId id,
        ProgramExerciseUpdate update,
        CancellationToken cancellationToken = default
    );

    /// <summary>Deletes a program exercise.</summary>
    /// <param name="programId">The owning program's identifier.</param>
    /// <param name="sessionId">The owning session's identifier.</param>
    /// <param name="sessionPhaseId">The owning session phase's identifier.</param>
    /// <param name="id">The program exercise's identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="DeleteProgramExerciseSucceeded"/> when the program exercise is
    /// deleted or was already gone, or a <see cref="DeleteProgramExerciseFailed"/>
    /// carrying an error message for any other non-success response or a transport/parse
    /// exception caught during the request.
    /// </returns>
    Task<DeleteProgramExerciseOutcome> DeleteProgramExerciseAsync(
        ProgramId programId,
        SessionId sessionId,
        SessionPhaseId sessionPhaseId,
        ProgramExerciseId id,
        CancellationToken cancellationToken = default
    );
}
