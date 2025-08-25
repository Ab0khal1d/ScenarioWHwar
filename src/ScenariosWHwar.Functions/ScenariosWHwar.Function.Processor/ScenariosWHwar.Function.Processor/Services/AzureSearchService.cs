using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScenariosWHwar.API.Core.Common.Configurations;
using ScenariosWHwar.API.Core.Common.Domain.Episodes;
using ScenariosWHwar.API.Core.Common.Interfaces;
using ScenariosWHwar.Function.Processor.Configuration;
using ScenariosWHwar.Function.Processor.Models;

namespace ScenariosWHwar.Function.Processor.Services;

/// <summary>
/// Azure Cognitive Search service implementation
/// Handles indexing and deletion of episode documents
/// </summary>
public class AzureSearchService : IAzureSearchService
{
    private readonly SearchClient _searchClient;
    private readonly AzureSearchConfig _config;
    private readonly AzureStorageConfig _storageConfig;
    private readonly ILogger<AzureSearchService> _logger;

    public AzureSearchService(
        SearchClient searchClient,
        IOptions<AzureSearchConfig> searchConfig,
        IOptions<AzureStorageConfig> storageConfig,
        ILogger<AzureSearchService> logger)
    {
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        _config = searchConfig?.Value ?? throw new ArgumentNullException(nameof(searchConfig));
        _storageConfig = storageConfig?.Value ?? throw new ArgumentNullException(nameof(storageConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<Success>> IndexEpisodeAsync(Episode episode, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Indexing episode {EpisodeId} in Azure Cognitive Search", episode.Id);

            var searchDocument = MapToSearchDocument(episode);

            var indexAction = IndexDocumentsAction.MergeOrUpload(searchDocument);
            var batch = IndexDocumentsBatch.Create(indexAction);

            var response = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

            if (response.Value.Results.Any(r => !r.Succeeded))
            {
                var failures = response.Value.Results.Where(r => !r.Succeeded);
                var errorMessages = failures.Select(f => f.ErrorMessage).ToList();

                _logger.LogError("Failed to index episode {EpisodeId}. Errors: {Errors}",
                    episode.Id, string.Join(", ", errorMessages));

                return Error.Failure("Search.IndexError", $"Failed to index episode: {string.Join(", ", errorMessages)}");
            }

            _logger.LogInformation("Successfully indexed episode {EpisodeId} in Azure Cognitive Search", episode.Id);
            return Result.Success;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Search request failed while indexing episode {EpisodeId}", episode.Id);
            return Error.Failure("Search.RequestFailed", $"Azure Search request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while indexing episode {EpisodeId}", episode.Id);
            return Error.Failure("Search.UnexpectedError", $"Unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Success>> DeleteEpisodeAsync(int episodeId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting episode {EpisodeId} from Azure Cognitive Search", episodeId);

            var deleteAction = IndexDocumentsAction.Delete("id", episodeId.ToString());
            var batch = IndexDocumentsBatch.Create(deleteAction);

            var response = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

            if (response.Value.Results.Any(r => !r.Succeeded))
            {
                var failures = response.Value.Results.Where(r => !r.Succeeded);
                var errorMessages = failures.Select(f => f.ErrorMessage).ToList();

                _logger.LogError("Failed to delete episode {EpisodeId}. Errors: {Errors}",
                    episodeId, string.Join(", ", errorMessages));

                return Error.Failure("Search.DeleteError", $"Failed to delete episode: {string.Join(", ", errorMessages)}");
            }

            _logger.LogInformation("Successfully deleted episode {EpisodeId} from Azure Cognitive Search", episodeId);
            return Result.Success;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Search request failed while deleting episode {EpisodeId}", episodeId);
            return Error.Failure("Search.RequestFailed", $"Azure Search request failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting episode {EpisodeId}", episodeId);
            return Error.Failure("Search.UnexpectedError", $"Unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Azure Cognitive Search health");

            // Perform a simple search to verify connectivity
            var searchOptions = new SearchOptions
            {
                Size = 1,
                IncludeTotalCount = false
            };

            await _searchClient.SearchAsync<EpisodeSearchDocument>("*", searchOptions, cancellationToken);

            _logger.LogDebug("Azure Cognitive Search health check passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Cognitive Search health check failed");
            return false;
        }
    }

    /// <summary>
    /// Maps an Episode domain entity to an EpisodeSearchDocument
    /// </summary>
    private EpisodeSearchDocument MapToSearchDocument(Episode episode)
    {
        return new EpisodeSearchDocument
        {
            Id = episode.Id.ToString(),
            Title = episode.Title,
            Description = episode.Description,
            Category = episode.Category.ToString(),
            Language = episode.Language,
            Duration = episode.Duration,
            PublishDate = episode.PublishDate,
            SourceUrl = episode.SourceUrl,
            SourceType = episode.SourceType.ToString(),
            CreatedAt = episode.CreatedAt,
            UpdatedAt = episode.UpdatedAt
        };
    }
}
