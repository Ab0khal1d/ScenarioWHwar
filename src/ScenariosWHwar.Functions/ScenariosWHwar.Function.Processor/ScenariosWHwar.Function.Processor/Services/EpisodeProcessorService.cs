using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScenariosWHwar.API.Core.Common.Domain.Episodes;
using ScenariosWHwar.Function.Processor.Configuration;
using ScenariosWHwar.Function.Processor.Data;
using ScenariosWHwar.Function.Processor.Models;
using System.Text.RegularExpressions;

namespace ScenariosWHwar.Function.Processor.Services;

/// <summary>
/// Service that handles episode processing business logic
/// Coordinates between database updates and search indexing
/// </summary>
public class EpisodeProcessorService : IEpisodeProcessorService
{
    private readonly IEpisodeRepository _episodeRepository;
    private readonly IAzureSearchService _searchService;
    private readonly ProcessorConfig _config;
    private readonly ILogger<EpisodeProcessorService> _logger;
    private readonly Regex _blobPathRegex;

    public EpisodeProcessorService(
        IEpisodeRepository episodeRepository,
        IAzureSearchService searchService,
        IOptions<ProcessorConfig> config,
        ILogger<EpisodeProcessorService> logger)
    {
        _episodeRepository = episodeRepository ?? throw new ArgumentNullException(nameof(episodeRepository));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _blobPathRegex = new Regex(_config.BlobPathPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public async Task<ErrorOr<Success>> ProcessBlobUploadAsync(Uri blobUri, long blobSize, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing blob upload for URI: {BlobUri}, Size: {BlobSize} bytes", blobUri, blobSize);

            // Extract episode ID from blob path
            var episodeIdResult = ExtractEpisodeIdFromBlobPath(blobUri.ToString());
            if (episodeIdResult.IsError)
            {
                _logger.LogWarning("Invalid blob path format: {BlobUri}. Expected pattern: {Pattern}",
                    blobUri, _config.BlobPathPattern);
                return episodeIdResult.Errors;
            }

            var episodeId = episodeIdResult.Value;
            _logger.LogDebug("Extracted episode ID {EpisodeId} from blob URI", episodeId);

            // Get episode from database
            var episodeResult = await _episodeRepository.GetByIdAsync(episodeId, cancellationToken);
            if (episodeResult.IsError)
            {
                _logger.LogError("Failed to retrieve episode {EpisodeId}: {Error}",
                    episodeId, string.Join(", ", episodeResult.Errors.Select(e => e.Description)));
                return episodeResult.Errors;
            }

            var episode = episodeResult.Value;

            // Validate episode state
            //if (episode.Status != EpisodeStatus.PendingUpload && episode.Status != EpisodeStatus.Processing)
            //{
            //    _logger.LogWarning("Episode {EpisodeId} has unexpected status {Status} for blob upload",
            //        episodeId, episode.Status);
            //    return Error.Validation("Episode.InvalidStatus",
            //        $"Episode {episodeId} has status {episode.Status}, expected PendingUpload or Processing");
            //}

            // Update episode status to Ready
            var updateStatusResult = episode.UpdateStatus(EpisodeStatus.Ready);
            if (updateStatusResult.IsError)
            {
                _logger.LogError("Failed to update episode {EpisodeId} status: {Error}",
                    episodeId, string.Join(", ", updateStatusResult.Errors.Select(e => e.Description)));
                return updateStatusResult.Errors;
            }

            // Save episode to database
            var saveResult = await _episodeRepository.UpdateAsync(episode, cancellationToken);
            if (saveResult.IsError)
            {
                _logger.LogError("Failed to save episode {EpisodeId} to database: {Error}",
                    episodeId, string.Join(", ", saveResult.Errors.Select(e => e.Description)));
                return saveResult.Errors;
            }

            // Index episode in Azure Cognitive Search
            var indexResult = await _searchService.IndexEpisodeAsync(episode, cancellationToken);
            if (indexResult.IsError)
            {
                _logger.LogError("Failed to index episode {EpisodeId} in search: {Error}",
                    episodeId, string.Join(", ", indexResult.Errors.Select(e => e.Description)));

                // This is a non-critical error - the episode is still ready, but won't be searchable
                _logger.LogWarning("Episode {EpisodeId} is ready but not searchable due to indexing failure", episodeId);
            }
            else
            {
                _logger.LogInformation("Successfully indexed episode {EpisodeId} in search", episodeId);
            }

            _logger.LogInformation("Successfully processed blob upload for episode {EpisodeId}. Status updated to Ready", episodeId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing blob upload for URI: {BlobUri}", blobUri);
            return Error.Failure("Processor.UnexpectedError", $"Unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Success>> ProcessBlobCreatedEventAsync(BlobCreatedEventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing blob created event for URL: {BlobUrl}", eventData.Url);

            // Extract episode ID from blob path
            var episodeIdResult = ExtractEpisodeIdFromBlobPath(eventData.Url);
            if (episodeIdResult.IsError)
            {
                _logger.LogWarning("Invalid blob path format: {BlobUrl}. Expected pattern: {Pattern}",
                    eventData.Url, _config.BlobPathPattern);
                return episodeIdResult.Errors;
            }

            var episodeId = episodeIdResult.Value;
            _logger.LogDebug("Extracted episode ID {EpisodeId} from blob path", episodeId);

            // Get episode from database
            var episodeResult = await _episodeRepository.GetByIdAsync(episodeId, cancellationToken);
            if (episodeResult.IsError)
            {
                _logger.LogError("Failed to retrieve episode {EpisodeId}: {Error}",
                    episodeId, string.Join(", ", episodeResult.Errors.Select(e => e.Description)));
                return episodeResult.Errors;
            }

            var episode = episodeResult.Value;

            // Validate episode state
            if (episode.Status != EpisodeStatus.PendingUpload && episode.Status != EpisodeStatus.Processing)
            {
                _logger.LogWarning("Episode {EpisodeId} has unexpected status {Status} for blob creation event",
                    episodeId, episode.Status);
                return Error.Validation("Episode.InvalidStatus",
                    $"Episode {episodeId} has status {episode.Status}, expected PendingUpload or Processing");
            }

            // Update episode status to Ready
            var updateStatusResult = episode.UpdateStatus(EpisodeStatus.Ready);
            if (updateStatusResult.IsError)
            {
                _logger.LogError("Failed to update episode {EpisodeId} status: {Error}",
                    episodeId, string.Join(", ", updateStatusResult.Errors.Select(e => e.Description)));
                return updateStatusResult.Errors;
            }

            // Save episode to database
            var saveResult = await _episodeRepository.UpdateAsync(episode, cancellationToken);
            if (saveResult.IsError)
            {
                _logger.LogError("Failed to save episode {EpisodeId} to database: {Error}",
                    episodeId, string.Join(", ", saveResult.Errors.Select(e => e.Description)));
                return saveResult.Errors;
            }

            // Index episode in Azure Cognitive Search
            var indexResult = await _searchService.IndexEpisodeAsync(episode, cancellationToken);
            if (indexResult.IsError)
            {
                _logger.LogError("Failed to index episode {EpisodeId} in search: {Error}",
                    episodeId, string.Join(", ", indexResult.Errors.Select(e => e.Description)));

                // This is a non-critical error - the episode is still ready, but won't be searchable
                // We log the error but don't fail the entire operation
                _logger.LogWarning("Episode {EpisodeId} is ready but not searchable due to indexing failure", episodeId);
            }
            else
            {
                _logger.LogInformation("Successfully indexed episode {EpisodeId} in search", episodeId);
            }

            _logger.LogInformation("Successfully processed blob created event for episode {EpisodeId}. Status updated to Ready", episodeId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing blob created event for URL: {BlobUrl}", eventData.Url);
            return Error.Failure("Processor.UnexpectedError", $"Unexpected error occurred: {ex.Message}");
        }
    }

    public ErrorOr<int> ExtractEpisodeIdFromBlobPath(string blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
        {
            return Error.Validation("BlobPathValue.Empty", "Blob URL cannot be empty");
        }

        try
        {
            // Extract the path part from the URL
            var uri = new Uri(blobUrl);
            var path = uri.AbsolutePath.TrimStart('/');

            _logger.LogDebug("Extracting episode ID from path: {Path}", path);

            var match = _blobPathRegex.Match(path);
            if (!match.Success)
            {
                return Error.Validation("BlobPathValue.InvalidFormat",
                    $"Blob path '{path}' does not match expected pattern '{_config.BlobPathPattern}'");
            }

            if (!int.TryParse(match.Groups[1].Value, out var episodeId))
            {
                return Error.Validation("BlobPathValue.InvalidEpisodeId",
                    $"Could not parse episode ID from path '{path}'");
            }

            if (episodeId <= 0)
            {
                return Error.Validation("BlobPathValue.InvalidEpisodeId",
                    $"Episode ID must be greater than 0, got {episodeId}");
            }

            return episodeId;
        }
        catch (UriFormatException)
        {
            return Error.Validation("BlobPathValue.InvalidUrl", $"Invalid URL format: {blobUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting episode ID from blob URL: {BlobUrl}", blobUrl);
            return Error.Failure("BlobPathValue.ExtractionError", $"Failed to extract episode ID: {ex.Message}");
        }
    }
}
