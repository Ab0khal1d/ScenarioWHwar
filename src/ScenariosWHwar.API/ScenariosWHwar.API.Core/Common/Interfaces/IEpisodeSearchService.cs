using System.Text.Json.Serialization;

namespace ScenariosWHwar.API.Core.Common.Interfaces;

/// <summary>
/// Represents a search filter for episodes.
/// </summary>
public record EpisodeSearchFilter(
    string? Query = null,
    string? Category = null,
    string? Language = null,
    int Page = 1,
    int PageSize = 20);

/// <summary>
/// Represents a paginated search result.
/// </summary>
public record PaginatedSearchResult<T>(
    IReadOnlyList<T> Results,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>
/// Represents an episode document in the search index.
/// </summary>
public class EpisodeSearchDocument
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public float Duration { get; set; }
    public DateTime PublishDate { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Service interface for episode search operations using Azure Cognitive Search.
/// </summary>
public interface IEpisodeSearchService
{
    /// <summary>
    /// Searches for episodes based on the provided filter criteria.
    /// </summary>
    /// <param name="filter">The search filter containing query parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A paginated result containing matching episodes.</returns>
    Task<ErrorOr<PaginatedSearchResult<EpisodeSearchDocument>>> SearchEpisodesAsync(
        EpisodeSearchFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific episode by its ID from the search index.
    /// </summary>
    /// <param name="id">The episode ID to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The episode document if found, otherwise an error.</returns>
    Task<ErrorOr<EpisodeSearchDocument>> GetEpisodeByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the search service is healthy and operational.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the service is healthy, false otherwise.</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
