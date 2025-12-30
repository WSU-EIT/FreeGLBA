namespace FreeGLBA.Client;

/// <summary>
/// Interface for the GLBA client. Use this interface for dependency injection and mocking.
/// </summary>
public interface IGlbaClient
{
    /// <summary>
    /// Logs a single GLBA access event.
    /// </summary>
    /// <param name="request">The event details to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the server.</returns>
    /// <exception cref="GlbaAuthenticationException">Thrown when the API key is invalid.</exception>
    /// <exception cref="GlbaValidationException">Thrown when the request data is invalid.</exception>
    /// <exception cref="GlbaDuplicateException">Thrown when a duplicate event is detected.</exception>
    /// <exception cref="GlbaException">Thrown for other server errors.</exception>
    Task<GlbaEventResponse> LogAccessAsync(GlbaEventRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a batch of GLBA access events.
    /// Maximum 1000 events per batch.
    /// </summary>
    /// <param name="requests">The events to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The batch response with counts of accepted/rejected events.</returns>
    /// <exception cref="GlbaAuthenticationException">Thrown when the API key is invalid.</exception>
    /// <exception cref="GlbaBatchTooLargeException">Thrown when more than 1000 events are submitted.</exception>
    /// <exception cref="GlbaException">Thrown for other server errors.</exception>
    Task<GlbaBatchResponse> LogAccessBatchAsync(IEnumerable<GlbaEventRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to log a GLBA access event without throwing exceptions.
    /// </summary>
    /// <param name="request">The event details to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the event was accepted or is a duplicate; false if an error occurred.</returns>
    Task<bool> TryLogAccessAsync(GlbaEventRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a data export event with simplified parameters.
    /// </summary>
    /// <param name="userId">The ID of the user performing the export.</param>
    /// <param name="subjectId">The ID of the data subject being exported.</param>
    /// <param name="purpose">The business purpose for the export.</param>
    /// <param name="userName">Optional display name of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the server.</returns>
    Task<GlbaEventResponse> LogExportAsync(
        string userId,
        string subjectId,
        string? purpose = null,
        string? userName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a data view event with simplified parameters.
    /// </summary>
    /// <param name="userId">The ID of the user viewing the data.</param>
    /// <param name="subjectId">The ID of the data subject being viewed.</param>
    /// <param name="userName">Optional display name of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the server.</returns>
    Task<GlbaEventResponse> LogViewAsync(
        string userId,
        string subjectId,
        string? userName = null,
        CancellationToken cancellationToken = default);
}
