using Microsoft.Extensions.Options;
using ScenariosWHwar.API.Core.Common.Configurations;
using ScenariosWHwar.API.Core.Common.Domain.Episodes.Events;

namespace ScenariosWHwar.CMS.API.Features.Episodes.EventHandlers;

public class EpisodeDeletedEventHandler : INotificationHandler<EpisodeDeletedEvent>
{
    private readonly IIntegrationEventsPublisher _serviceBusPublisher;
    private readonly ServiceBusConfig _config;
    private readonly ILogger<EpisodeDeletedEventHandler> _logger;

    public EpisodeDeletedEventHandler(
        IIntegrationEventsPublisher serviceBusPublisher,
        IOptions<ServiceBusConfig> config,
        ILogger<EpisodeDeletedEventHandler> logger)
    {
        _serviceBusPublisher = serviceBusPublisher;
        _config = config.Value;
        _logger = logger;
    }

    public async Task Handle(EpisodeDeletedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Publishing episode deleted event for episode {EpisodeId}",
                notification.EpisodeId);

            // send message to service bus
            var integrationEvent = new EpisodeDeletedIntegrationEvent
            {
                EpisodeId = notification.EpisodeId,
                BlobPath = notification.BlobPath,
            };

            await _serviceBusPublisher.PublishAsync(
                integrationEvent,
                _config.ProcessorQueueName,
                cancellationToken);



            _logger.LogInformation(
                "Successfully published episode deleted event for episode {EpisodeId}",
                notification.EpisodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish episode deleted event for episode {EpisodeId}",
                notification.EpisodeId);
        }
    }
}