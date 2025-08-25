using ScenariosWHwar.API.Core.Common.Domain.Episodes;

namespace ScenariosWHwar.Function.Processor.Extensions;

/// <summary>
/// Extension methods for Episode entity
/// </summary>
public static class EpisodeExtensions
{
    /// <summary>
    /// Checks if the episode can be processed for blob upload
    /// </summary>
    /// <param name="episode">Episode to check</param>
    /// <returns>True if the episode can be processed</returns>
    public static bool CanProcessBlobUpload(this Episode episode)
    {
        return episode.Status == EpisodeStatus.PendingUpload ||
               episode.Status == EpisodeStatus.Processing;
    }

    /// <summary>
    /// Gets a human-readable status description
    /// </summary>
    /// <param name="status">Episode status</param>
    /// <returns>Human-readable status description</returns>
    public static string GetDisplayName(this EpisodeStatus status)
    {
        return status switch
        {
            EpisodeStatus.PendingUpload => "Pending Upload",
            EpisodeStatus.Processing => "Processing",
            EpisodeStatus.Ready => "Ready",
            EpisodeStatus.Deleting => "Deleting",
            EpisodeStatus.Failed => "Failed",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Checks if the episode is in a final state (no further processing expected)
    /// </summary>
    /// <param name="status">Episode status</param>
    /// <returns>True if the status is final</returns>
    public static bool IsFinalState(this EpisodeStatus status)
    {
        return status is EpisodeStatus.Ready or EpisodeStatus.Failed;
    }

    /// <summary>
    /// Checks if the episode is in a processing state
    /// </summary>
    /// <param name="status">Episode status</param>
    /// <returns>True if the status indicates processing</returns>
    public static bool IsProcessingState(this EpisodeStatus status)
    {
        return status is EpisodeStatus.PendingUpload or EpisodeStatus.Processing;
    }
}
