using ErrorOr;
using ScenariosWHwar.Function.Processor.Models;

namespace ScenariosWHwar.Function.Processor.Services;

/// <summary>
/// Interface for episode processing business logic
/// </summary>
public interface IEpisodeProcessorService
{
    /// <summary>
    /// Processes a blob upload for an episode (blob trigger version)
    /// Updates episode status and indexes it for search
    /// </summary>
    /// <param name="blobUri">URI of the uploaded blob</param>
    /// <param name="blobSize">Size of the uploaded blob in bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<ErrorOr<Success>> ProcessBlobUploadAsync(Uri blobUri, long blobSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a blob created event for an episode (Event Grid version)
    /// Updates episode status and indexes it for search
    /// </summary>
    /// <param name="eventData">Blob created event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<ErrorOr<Success>> ProcessBlobCreatedEventAsync(BlobCreatedEventData eventData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts episode ID from blob path
    /// </summary>
    /// <param name="blobUrl">Blob URL or path</param>
    /// <returns>Episode ID if valid, otherwise error</returns>
    ErrorOr<int> ExtractEpisodeIdFromBlobPath(string blobUrl);
}
