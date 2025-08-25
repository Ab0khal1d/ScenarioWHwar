

namespace ScenariosWHwar.API.Core.Common.Domain.Episodes;

[ValueObject<string>]
public readonly partial struct EpisodeTitle
{
    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid(EpisodeErrors.Validation.TitleRequired.Description);

        if (value.Length > 500)
            return Validation.Invalid(EpisodeErrors.Validation.TitleNotExceed500Char.Description);

        return Validation.Ok;
    }
}

[ValueObject<string>]
public readonly partial struct EpisodeDescription
{
    private static Validation Validate(string value)
    {
        if (value?.Length > 2000)
            return Validation.Invalid(EpisodeErrors.Validation.DescriptionNotExceed2000Char.Description);

        return Validation.Ok;
    }
}

[ValueObject<string>]
public readonly partial struct BlobPathValue
{
    private static Validation Validate(string value)
    {
        if (value?.Length > 1000)
            return Validation.Invalid(EpisodeErrors.Validation.BlobPathNotExceed1000Char.Description);

        return Validation.Ok;
    }
}

[ValueObject<string>]
public readonly partial struct EpisodeFormat
{
    public static readonly EpisodeFormat Mp3 = From("mp3");
    public static readonly EpisodeFormat Mp4 = From("mp4");

    private static Validation Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Validation.Invalid(EpisodeErrors.Validation.FormatInvalid.Description);

        var normalizedValue = value.ToLowerInvariant();
        if (normalizedValue is not "mp3" and not "mp4")
            return Validation.Invalid(EpisodeErrors.Validation.FormatInvalid.Description);

        return Validation.Ok;
    }

    public bool IsAudio => Value.Equals("mp3", StringComparison.InvariantCultureIgnoreCase);
    public bool IsVideo => Value.Equals("mp4", StringComparison.InvariantCultureIgnoreCase);

    public string GetFileExtension()
    {
        return Value.ToLowerInvariant();
    }

    public string GetMimeType()
    {
        return Value.ToLowerInvariant() switch
        {
            "mp3" => "audio/mpeg",
            "mp4" => "video/mp4",
            _ => "application/octet-stream"
        };
    }
}
public enum EpisodeCategory
{
    Technology,
    Culture,
    History,
    Science,
    Politics,
    Sports,
    Entertainment,
    Education,
    Business,
    Health
}

public enum EpisodeStatus
{
    PendingUpload,
    Processing,
    Ready,
    Deleting,
    Failed
}

public enum SourceType
{
    DirectUpload,
    YoutubeImport,
    RssImport
}
