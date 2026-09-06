using Trainfree.Domain.Ids;

namespace Trainfree.Admin.Admin;

/// <summary>Client for the Worker's <c>/api/exercises</c> endpoints.</summary>
internal interface IExercisesApiClient
{
    /// <summary>Retrieves all exercises in creation order.</summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    Task<IReadOnlyList<ExerciseSummary>> GetExercisesAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates a new exercise with the given name.</summary>
    /// <param name="name">The exercise name, 4-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="CreateExerciseSucceeded"/> on success, or a <see cref="CreateExerciseFailed"/>
    /// carrying an error message when the server rejects the request (e.g. a duplicate name)
    /// or a transport/parse exception is caught.
    /// </returns>
    Task<CreateExerciseOutcome> CreateExerciseAsync(
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Renames an existing exercise.</summary>
    /// <param name="id">The exercise's identifier.</param>
    /// <param name="name">The new name, 4-100 characters after trimming.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="RenameExerciseSucceeded"/> on success, or a <see cref="RenameExerciseFailed"/>
    /// carrying an error message when the server rejects the request or a transport/parse
    /// exception is caught.
    /// </returns>
    Task<RenameExerciseOutcome> RenameExerciseAsync(
        ExerciseId id,
        string name,
        CancellationToken cancellationToken = default
    );

    /// <summary>Deletes an exercise.</summary>
    /// <param name="id">The exercise's identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>
    /// A <see cref="DeleteExerciseSucceeded"/> when the exercise is deleted or was already
    /// gone, or a <see cref="DeleteExerciseFailed"/> carrying an error message for any other
    /// non-success response or a transport/parse exception caught during the request.
    /// </returns>
    Task<DeleteExerciseOutcome> DeleteExerciseAsync(
        ExerciseId id,
        CancellationToken cancellationToken = default
    );
}
