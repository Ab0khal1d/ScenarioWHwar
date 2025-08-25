using ErrorOr;
using ScenariosWHwar.API.Core.Common.Domain.Episodes;

namespace ScenariosWHwar.Function.Processor.Data;

/// <summary>
/// Repository interface for Episode data access operations
/// </summary>
public interface IEpisodeRepository
{
    /// <summary>
    /// Gets an episode by its ID
    /// </summary>
    /// <param name="id">Episode ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Episode if found, otherwise error</returns>
    Task<ErrorOr<Episode>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an episode entity
    /// </summary>
    /// <param name="episode">Episode to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<ErrorOr<Success>> UpdateAsync(Episode episode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets episodes that are ready for publishing (status = Ready and publish date <= now)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of episodes ready for publishing</returns>
    Task<List<Episode>> GetReadyForPublishingAsync(CancellationToken cancellationToken = default);
}
