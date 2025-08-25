using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ScenariosWHwar.Function.Processor.Services;

namespace ScenariosWHwar.Function.Processor;

/// <summary>
/// Azure Function that processes blob trigger events when videos are uploaded
/// Handles episode video upload completion and updates episode status
///
/// BLOB TRIGGER SETUP:
/// - Container: "videos"
/// - Path pattern: "videos/{name}" (where name = "episode-{id}.{ext}")
/// - Connection: "AzureWebJobsStorage" (configured in local.settings.json)
/// - Expected blob naming: "videos/episode-123.mp4" or "videos/episode-456.mp3"
///
/// TESTING:
/// 1. Set AzureWebJobsStorage in local.settings.json to your storage account
/// 2. Upload a file named "episode-123.mp4" to the "videos" container
/// 3. Ensure episode with ID 123 exists in database with status "PendingUpload"
/// 4. Function will trigger automatically and update episode status to "Ready"
/// </summary>
public class EpisodeProcessorFunction
{
    private readonly IEpisodeProcessorService _processorService;
    private readonly ILogger<EpisodeProcessorFunction> _logger;

    public EpisodeProcessorFunction(
        IEpisodeProcessorService processorService,
        ILogger<EpisodeProcessorFunction> logger)
    {
        _processorService = processorService ?? throw new ArgumentNullException(nameof(processorService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Blob triggered function that processes uploaded episode videos
    /// Triggered when a new video file is uploaded to the videos container
    /// </summary>
    /// <param name="blobStream">Stream of the uploaded blob</param>
    /// <param name="name">Name of the blob (includes path)</param>
    /// <param name="uri">URI of the uploaded blob</param>
    [Function(nameof(ProcessUploadedVideo))]
    public async Task ProcessUploadedVideo(
        [BlobTrigger("episodes/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream,
        string name,
        Uri uri)
    {
        try
        {
            _logger.LogInformation("Processing uploaded video blob: {BlobName} at {BlobUri}", name, uri);

            // Extract episode ID from blob path first to validate it's a valid episode blob
            var episodeIdResult = _processorService.ExtractEpisodeIdFromBlobPath(uri.ToString());
            if (episodeIdResult.IsError)
            {
                var errorMessages = string.Join(", ", episodeIdResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Invalid blob path format for {BlobName}: {Errors}", name, errorMessages);
                return; // Don't throw for invalid blob names - just ignore them
            }

            var episodeId = episodeIdResult.Value;
            _logger.LogDebug("Processing episode {EpisodeId} from blob {BlobName}", episodeId, name);

            // Process the blob upload using the streamlined method
            var result = await _processorService.ProcessBlobUploadAsync(uri, blobStream.Length);
            if (result.IsError)
            {
                var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to process uploaded video for episode {EpisodeId}: {Errors}",
                    episodeId, errorMessages);

                // Throw exception to trigger retry mechanism
                throw new InvalidOperationException($"Failed to process video upload for episode {episodeId}: {errorMessages}");
            }

            _logger.LogInformation("Successfully processed video upload for episode {EpisodeId}", episodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing uploaded video blob: {BlobName}", name);
            throw; // Rethrow to trigger retry
        }
    }
}