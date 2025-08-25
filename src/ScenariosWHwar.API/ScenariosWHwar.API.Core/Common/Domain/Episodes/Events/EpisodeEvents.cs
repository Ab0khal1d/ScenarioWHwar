namespace ScenariosWHwar.API.Core.Common.Domain.Episodes.Events;

public record EpisodeCreatedEvent(
    int EpisodeId,
    string Title,
    string Description,
    EpisodeCategory Category,
    string Format,
    string Language,
    DateTime CreatedAt) : IDomainEvent;

public record EpisodeBlobPathUpdatedEvent(
    int EpisodeId,
    string? OldBlobPath,
    string NewBlobPath,
    DateTime UpdatedAt) : IDomainEvent;

public record EpisodeUpdatedEvent(
    int EpisodeId,
    string Title,
    string Description,
    EpisodeCategory Category,
    string Language,
    float Duration,
    DateTime PublishDate,
    DateTime UpdatedAt) : IDomainEvent;

public record EpisodeDeletedEvent(
    int EpisodeId,
    string? BlobPath,
    DateTime DeletedAt) : IDomainEvent;

public record EpisodeStatusChangedEvent(
    int EpisodeId,
    EpisodeStatus OldStatus,
    EpisodeStatus NewStatus,
    DateTime ChangedAt) : IDomainEvent;

public record EpisodeImportInitiatedEvent(
    int EpisodeId,
    SourceType SourceType,
    string SourceUrl,
    DateTime StartedAt) : IDomainEvent;