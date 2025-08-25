namespace ScenariosWHwar.API.Core.Common.Domain.Episodes.Events;
public class EpisodeDeletedIntegrationEvent
{
    public int EpisodeId { get; set; }
    public string? BlobPath { get; set; }
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; } = "EpisodeDeleted";
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
