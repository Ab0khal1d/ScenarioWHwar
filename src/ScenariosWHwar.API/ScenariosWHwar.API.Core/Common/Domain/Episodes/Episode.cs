using ScenariosWHwar.API.Core.Common.Domain.Episodes.Events;

namespace ScenariosWHwar.API.Core.Common.Domain.Episodes;
[ValueObject<int>]
public readonly partial struct EpisodeId;

public class Episode : AggregateRoot<int>
{
    private EpisodeTitle _title;
    private EpisodeDescription _description;
    private EpisodeCategory _category;
    private EpisodeFormat _format;
    private string _language;
    private string _sourceUrl;
    private float _duration;
    private DateTime _publishDate;
    private EpisodeStatus _status;
    private BlobPathValue? _blobPath;
    private SourceType _sourceType;
    private DateTime _createdAt;
    private DateTime _updatedAt;

    // Public properties
    public string Title
    {
        get => _title.Value;
        set
        {
            _title = EpisodeTitle.From(value);
        }
    }
    public string Description
    {
        get => _description.Value;
        set
        {
            _description = EpisodeDescription.From(value);
        }
    }
    public EpisodeCategory Category => _category;
    public string Format
    {
        get => _format.Value;
        set
        {
            _format = EpisodeFormat.From(value);
        }
    }
    public string Language => _language;
    public string SourceUrl => _sourceUrl;
    public float Duration => _duration;
    public DateTime PublishDate => _publishDate;
    public EpisodeStatus Status => _status;
    public string? BlobPath
    {
        get => _blobPath?.Value;
        set
        {
            if (value is null)
            {
                _blobPath = null;
            }
            else
            {
                _blobPath = BlobPathValue.From(value);
            }
        }
    }
    public SourceType SourceType => _sourceType;
    public new DateTime CreatedAt => _createdAt;
    public new DateTime UpdatedAt => _updatedAt;


    private Episode() { } // Needed for EF Core

    private Episode(int id) : base(id)
    {
        _language = "ar"; // Default to Arabic
        _status = EpisodeStatus.PendingUpload;
        _createdAt = DateTime.UtcNow;
        _updatedAt = DateTime.UtcNow;
    }

    public static Episode Create(
        EpisodeTitle title,
        EpisodeDescription description,
        EpisodeCategory category,
        EpisodeFormat format,
        string? language = null,
        string url = "",
        SourceType sourceType = SourceType.DirectUpload,
        float duration = 0,
        DateTime? publishDate = null)
    {
        var episode = new Episode() // EF will set the actual ID
        {
            _title = title,
            _description = description,
            _category = category,
            _format = format,
            _language = language ?? "ar",
            _sourceUrl = url,
            _duration = duration,
            _sourceType = sourceType,
            _publishDate = publishDate ?? DateTime.UtcNow,
            _status = EpisodeStatus.PendingUpload,
            _createdAt = DateTime.UtcNow,
            _updatedAt = DateTime.UtcNow,
        };

        episode.AddDomainEvent(new EpisodeCreatedEvent(
            episode.Id,
            episode.Title,
            episode.Description,
            episode.Category,
            episode.Format,
            episode.Language,
            episode.CreatedAt));

        return episode;
    }

    public ErrorOr<Success> UpdateMetadata(
        EpisodeTitle title,
        EpisodeDescription description,
        EpisodeCategory category,
        DateTime publishDate)
    {
        _title = title;
        _description = description;
        _category = category;
        _publishDate = publishDate;
        _updatedAt = DateTime.UtcNow;

        AddDomainEvent(new EpisodeUpdatedEvent(
            Id,
            Title,
            Description,
            Category,
            Language,
            Duration,
            PublishDate,
            UpdatedAt));

        return Result.Success;
    }

    public ErrorOr<Success> UpdateStatus(EpisodeStatus newStatus)
    {
        if (_status == newStatus)
            return EpisodeErrors.Business.CantUpateSameStatus;

        if (_status == EpisodeStatus.Processing)
            return EpisodeErrors.Business.CannotUpdateProcessingEpisode;

        if (newStatus == EpisodeStatus.Deleting)
            return EpisodeErrors.Business.CannotUpdateEpisodeToDeleting;

        var oldStatus = _status;
        _status = newStatus;
        _updatedAt = DateTime.UtcNow;

        AddDomainEvent(new EpisodeStatusChangedEvent(
            Id,
            oldStatus,
            newStatus,
            UpdatedAt));

        return Result.Success;
    }

    public ErrorOr<Success> UpdateBlobPath(BlobPathValue blobPath)
    {
        var oldBlobPath = _blobPath?.Value;
        _blobPath = blobPath;
        _updatedAt = DateTime.UtcNow;

        AddDomainEvent(new EpisodeBlobPathUpdatedEvent(
            Id,
            oldBlobPath,
            blobPath.Value,
            UpdatedAt));

        return Result.Success;
    }

    public ErrorOr<Success> NotifyImportProcessor()
    {
        _status = EpisodeStatus.Processing;
        _updatedAt = DateTime.UtcNow;

        AddDomainEvent(new EpisodeImportInitiatedEvent(
            Id,
            SourceType,
            SourceUrl,
            UpdatedAt));

        return Result.Success;
    }

    public ErrorOr<Success> MarkForDeletion()
    {
        _status = EpisodeStatus.Deleting;
        _updatedAt = DateTime.UtcNow;

        AddDomainEvent(new EpisodeDeletedEvent(
            Id,
            BlobPath,
            DateTime.UtcNow));

        return Result.Success;
    }

    public bool CanBePublished()
    {
        return Status == EpisodeStatus.Ready && PublishDate <= DateTime.UtcNow;
    }

    public bool IsProcessing()
    {
        return Status == EpisodeStatus.Processing;
    }

    public bool HasPath()
    {
        return !string.IsNullOrWhiteSpace(BlobPath);
    }

    public string GenerateBlobPath()
    {
        return $"/{Id}.{_format.GetFileExtension()}";
    }

    public bool RequiresVideoProcessing()
    {
        return _format.IsVideo;
    }

    public bool RequiresAudioProcessing()
    {
        return _format.IsAudio;
    }
}
