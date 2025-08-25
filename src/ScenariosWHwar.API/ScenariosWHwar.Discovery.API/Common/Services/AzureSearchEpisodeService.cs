using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using ScenariosWHwar.API.Core.Common.Configurations;

namespace ScenariosWHwar.Discovery.API.Common.Services;


/// <summary>
/// Azure Cognitive Search implementation of the episode search service.
/// </summary>
public class AzureSearchEpisodeService : IEpisodeSearchService
{
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _indexClient;
    private readonly ILogger<AzureSearchEpisodeService> _logger;
    private readonly AzureSearchConfig _options;

    public AzureSearchEpisodeService(
        IOptions<AzureSearchConfig> options,
        ILogger<AzureSearchEpisodeService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Validate configuration
        if (string.IsNullOrEmpty(_options.ServiceEndpoint) ||
            string.IsNullOrEmpty(_options.AdminApiKey) ||
            string.IsNullOrEmpty(_options.IndexName))
        {
            throw new InvalidOperationException(
                "Azure Search configuration is incomplete. Please check ServiceName, ApiKey, and IndexName.");
        }

        var credential = new AzureKeyCredential(_options.AdminApiKey);
        var serviceEndpoint = new Uri(_options.ServiceEndpoint);

        _searchClient = new SearchClient(serviceEndpoint, _options.IndexName, credential);
        _indexClient = new SearchIndexClient(serviceEndpoint, credential);
    }

    /// <inheritdoc />
    public async Task<ErrorOr<PaginatedSearchResult<EpisodeSearchDocument>>> SearchEpisodesAsync(
        EpisodeSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchOptions = new SearchOptions
            {
                Size = filter.PageSize,
                Skip = (filter.Page - 1) * filter.PageSize,
                IncludeTotalCount = true,
                OrderBy = { "PublishDate desc" } // Order by publish date descending - using correct casing
            };

            // Build the search query
            var searchText = BuildSearchQuery(filter);
            var filterExpression = BuildFilterExpression(filter);

            if (!string.IsNullOrEmpty(filterExpression))
                searchOptions.Filter = filterExpression;

            _logger.LogInformation(
                "Searching episodes with query: '{SearchText}', filter: '{Filter}', page: {Page}, pageSize: {PageSize}",
                searchText, filterExpression, filter.Page, filter.PageSize);

            var searchResults = await _searchClient.SearchAsync<EpisodeSearchDocument>(
                searchText,
                searchOptions,
                cancellationToken);

            var results = new List<EpisodeSearchDocument>();
            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                results.Add(result.Document);
            }

            var paginatedResult = new PaginatedSearchResult<EpisodeSearchDocument>(
                results,
                (int)(searchResults.Value.TotalCount ?? 0),
                filter.Page,
                filter.PageSize);

            _logger.LogInformation(
                "Search completed successfully. Found {TotalCount} total results, returning {ResultCount} on page {Page}",
                paginatedResult.TotalCount, results.Count, filter.Page);

            return paginatedResult;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Search request failed while searching episodes");
            return Error.Failure("Search.RequestFailed", "Failed to search episodes due to a search service error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while searching episodes");
            return Error.Failure("Search.UnexpectedError", "An unexpected error occurred while searching episodes");
        }
    }

    /// <inheritdoc />
    public async Task<ErrorOr<EpisodeSearchDocument>> GetEpisodeByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting episode by ID: {EpisodeId}", id);

            var searchOptions = new SearchOptions
            {
                Filter = $"Id eq '{id}' and Status eq 'Ready'", // Using correct casing for both Id and Status
                Size = 1
            };

            var searchResults = await _searchClient.SearchAsync<EpisodeSearchDocument>(
                "*",
                searchOptions,
                cancellationToken);

            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                _logger.LogInformation("Episode found with ID: {EpisodeId}", id);
                return result.Document;
            }

            _logger.LogWarning("Episode not found with ID: {EpisodeId}", id);
            return Error.NotFound("Episode.NotFound", $"Episode with ID {id} was not found or is not available");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Search request failed while getting episode by ID: {EpisodeId}", id);
            return Error.Failure("Search.RequestFailed", "Failed to retrieve episode due to a search service error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting episode by ID: {EpisodeId}", id);
            return Error.Failure("Search.UnexpectedError", "An unexpected error occurred while retrieving the episode");
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get index statistics as a health check
            var indexStats = await _indexClient.GetIndexStatisticsAsync(_options.IndexName, cancellationToken);
            _logger.LogDebug("Search service health check passed. Index has {DocumentCount} documents",
                indexStats.Value.DocumentCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Search service health check failed");
            return false;
        }
    }

    /// <summary>
    /// Builds the search query text based on the filter parameters.
    /// </summary>
    private static string BuildSearchQuery(EpisodeSearchFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.Query))
            return "*"; // Return all documents when no query is specified

        // Use the query text directly - Azure Search will handle tokenization and scoring
        return filter.Query;
    }

    /// <summary>
    /// Builds the OData filter expression for non-text filters.
    /// </summary>
    private static string BuildFilterExpression(EpisodeSearchFilter filter)
    {
        var filters = new List<string>
        {
            "Status eq 'Ready'", // Only show ready episodes - using correct casing
            $"PublishDate le {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}" // Only show published episodes - using correct casing
        };

        if (!string.IsNullOrWhiteSpace(filter.Category))
            filters.Add($"Category eq '{filter.Category.Replace("'", "''")}'"); // Using correct casing

        if (!string.IsNullOrWhiteSpace(filter.Language))
            filters.Add($"Language eq '{filter.Language.Replace("'", "''")}'"); // Using correct casing

        return string.Join(" and ", filters);
    }
}
