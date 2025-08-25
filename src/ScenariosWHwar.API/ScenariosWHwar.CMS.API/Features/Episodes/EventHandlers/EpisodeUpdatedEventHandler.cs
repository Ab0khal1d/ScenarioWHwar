using Microsoft.Extensions.Options;
using ScenariosWHwar.API.Core.Common.Configurations;
using ScenariosWHwar.API.Core.Common.Domain.Episodes.Events;

namespace ScenariosWHwar.CMS.API.Features.Episodes.EventHandlers;

public class EpisodeUpdatedEventHandler : INotificationHandler<EpisodeUpdatedEvent>
{
    private readonly IIntegrationEventsPublisher _serviceBusPublisher;
    private readonly ServiceBusConfig _config;
    private readonly ILogger<EpisodeUpdatedEventHandler> _logger;

    public EpisodeUpdatedEventHandler(
        IIntegrationEventsPublisher serviceBusPublisher,
        IOptions<ServiceBusConfig> config,
        ILogger<EpisodeUpdatedEventHandler> logger)
    {
        _serviceBusPublisher = serviceBusPublisher;
        _config = config.Value;
        _logger = logger;
    }

    public async Task Handle(EpisodeUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Publishing episode updated event for episode {EpisodeId}",
                notification.EpisodeId);

            var integrationEvent = new EpisodeUpdatedIntegrationEvent
            {
                EpisodeId = notification.EpisodeId,
                Title = notification.Title,
                Description = notification.Description,
                Category = notification.Category.ToString(),
                Language = notification.Language,
                Duration = notification.Duration,
                PublishDate = notification.PublishDate,
                UpdatedAt = notification.UpdatedAt,
                EventId = Guid.NewGuid(),
                EventType = "EpisodeUpdated",
                OccurredAt = DateTime.UtcNow
            };

            await _serviceBusPublisher.PublishAsync(
                integrationEvent,
                _config.ProcessorQueueName,
                cancellationToken);

            _logger.LogInformation(
                "Successfully published episode updated event for episode {EpisodeId}",
                notification.EpisodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish episode updated event for episode {EpisodeId}",
                notification.EpisodeId);
        }
    }
}
internal class EpisodeUpdatedIntegrationEvent
{
    public int EpisodeId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string Language { get; set; }
    public float Duration { get; set; }
    public DateTime PublishDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; }
    public DateTime OccurredAt { get; set; }
}