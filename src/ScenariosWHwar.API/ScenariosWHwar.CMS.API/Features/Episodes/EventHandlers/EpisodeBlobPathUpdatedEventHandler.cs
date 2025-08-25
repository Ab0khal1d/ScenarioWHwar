using ScenariosWHwar.API.Core.Common.Domain.Episodes.Events;

namespace ScenariosWHwar.CMS.API.Features.Episodes.EventHandlers;

public class EpisodeBlobPathUpdatedEventHandler : INotificationHandler<EpisodeBlobPathUpdatedEvent>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<EpisodeBlobPathUpdatedEvent> _logger;

    public EpisodeBlobPathUpdatedEventHandler(
        ApplicationDbContext dbContext,
        ILogger<EpisodeBlobPathUpdatedEvent> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(EpisodeBlobPathUpdatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Publishing episode update blob path event for episode {EpisodeId}",
                notification.EpisodeId);

            // Update blob path in the database
            var episodeId = notification.EpisodeId;
            var episodeBlobPath = notification.NewBlobPath;

            await _dbContext.Episodes
                .Where(e => e.Id == episodeId)
                .ExecuteUpdateAsync(e => e.SetProperty(ep => ep.BlobPath,
                episodeBlobPath), cancellationToken);


            _logger.LogInformation(
                "Successfully published episode update blob path event for episode {EpisodeId}",
                notification.EpisodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish episode update blob path event for episode {EpisodeId}",
                notification.EpisodeId);
            // Don't rethrow - this is an async operation that shouldn't fail the main flow
        }
    }
}

//public class EpisodeCreatedEventHandler : INotificationHandler<EpisodeCreatedEvent>
//{
//    private readonly IServiceBusPublisher _serviceBusPublisher;
//    private readonly ServiceBusConfig _config;
//    private readonly ILogger<EpisodeCreatedEventHandler> _logger;

//    public EpisodeCreatedEventHandler(
//        IServiceBusPublisher serviceBusPublisher,
//        IOptions<ServiceBusConfig> config,
//        ILogger<EpisodeCreatedEventHandler> logger)
//    {
//        _serviceBusPublisher = serviceBusPublisher;
//        _config = config.Value;
//        _logger = logger;
//    }

//    public async Task Handle(EpisodeCreatedEvent notification, CancellationToken cancellationToken)
//    {
//        try
//        {
//            _logger.LogInformation(
//                "Publishing episode created event for episode {EpisodeId}",
//                notification.EpisodeId);

//            // Create integration event for external systems
//            var integrationEvent = new EpisodeCreatedIntegrationEvent
//            {
//                EpisodeId = notification.EpisodeId,
//                Title = notification.Title,
//                Description = notification.Description,
//                Category = notification.Category.ToString(),
//                Language = notification.Language,
//                CreatedAt = notification.CreatedAt,
//                EventId = Guid.NewGuid(),
//                EventType = "EpisodeCreated",
//                OccurredAt = DateTime.UtcNow
//            };

//            await _serviceBusPublisher.PublishAsync(
//                integrationEvent,
//                _config.ProcessorQueueName,
//                cancellationToken);

//            _logger.LogInformation(
//                "Successfully published episode created event for episode {EpisodeId}",
//                notification.EpisodeId);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex,
//                "Failed to publish episode created event for episode {EpisodeId}",
//                notification.EpisodeId);
//            // Don't rethrow - this is an async operation that shouldn't fail the main flow
//        }
//    }
//}
