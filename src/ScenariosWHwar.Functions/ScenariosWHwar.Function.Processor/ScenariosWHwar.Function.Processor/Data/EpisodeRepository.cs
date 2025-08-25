using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScenariosWHwar.API.Core.Common.Domain.Episodes;
using ScenariosWHwar.CMS.API.Common.Persistence;

namespace ScenariosWHwar.Function.Processor.Data;

/// <summary>
/// Repository implementation for Episode data access operations
/// </summary>
public class EpisodeRepository : IEpisodeRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EpisodeRepository> _logger;

    public EpisodeRepository(ApplicationDbContext context, ILogger<EpisodeRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<Episode>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving episode with ID: {EpisodeId}", id);

            var episode = await _context.Episodes
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            if (episode is null)
            {
                _logger.LogWarning("Episode with ID {EpisodeId} not found", id);
                return Error.NotFound("Episode.NotFound", $"Episode with ID {id} was not found");
            }

            _logger.LogDebug("Successfully retrieved episode {EpisodeId} with status {Status}",
                id, episode.Status);

            return episode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving episode with ID {EpisodeId}", id);
            return Error.Failure("Episode.DatabaseError", "Failed to retrieve episode from database");
        }
    }

    public async Task<ErrorOr<Success>> UpdateAsync(Episode episode, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating episode {EpisodeId} with status {Status}",
                episode.Id, episode.Status);

            _context.Episodes.Update(episode);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated episode {EpisodeId} to status {Status}",
                episode.Id, episode.Status);

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating episode {EpisodeId}", episode.Id);
            return Error.Failure("Episode.UpdateError", "Failed to update episode in database");
        }
    }

    public async Task<List<Episode>> GetReadyForPublishingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving episodes ready for publishing");

            var episodes = await _context.Episodes
                .Where(e => e.Status == EpisodeStatus.Ready && e.PublishDate <= DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Found {Count} episodes ready for publishing", episodes.Count);

            return episodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving episodes ready for publishing");
            return new List<Episode>();
        }
    }
}
