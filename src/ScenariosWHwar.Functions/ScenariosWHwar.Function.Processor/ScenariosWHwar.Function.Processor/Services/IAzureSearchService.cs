using ErrorOr;
using ScenariosWHwar.API.Core.Common.Domain.Episodes;
using ScenariosWHwar.Function.Processor.Models;

namespace ScenariosWHwar.Function.Processor.Services;

/// <summary>
/// Interface for Azure Cognitive Search operations
/// </summary>
public interface IAzureSearchService
{
    /// <summary>
    /// Indexes or updates an episode document in Azure Cognitive Search
    /// </summary>
    /// <param name="episode">Episode to index</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<ErrorOr<Success>> IndexEpisodeAsync(Episode episode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an episode document from Azure Cognitive Search
    /// </summary>
    /// <param name="episodeId">ID of episode to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<ErrorOr<Success>> DeleteEpisodeAsync(int episodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the search service is healthy and accessible
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if healthy, false otherwise</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
