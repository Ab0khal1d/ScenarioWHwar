namespace ScenariosWHwar.Function.Processor.Configuration;

/// <summary>
/// Configuration for function processing behavior
/// </summary>
public class ProcessorConfig
{
    public const string SectionName = "Processor";

    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public bool EnableDetailedLogging { get; set; } = true;
    public string BlobPathPattern { get; set; } = @"^episodes/(\d+)\.(mp4|mp3)$";
}
