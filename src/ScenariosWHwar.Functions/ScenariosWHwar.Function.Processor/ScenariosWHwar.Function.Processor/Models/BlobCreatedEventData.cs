using System.Text.Json.Serialization;

namespace ScenariosWHwar.Function.Processor.Models;

/// <summary>
/// Event data for Azure Blob Storage creation events from Event Grid
/// </summary>
public class BlobCreatedEventData
{
    [JsonPropertyName("api")]
    public string Api { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("contentLength")]
    public long ContentLength { get; set; }

    [JsonPropertyName("blobType")]
    public string BlobType { get; set; } = string.Empty;

    [JsonPropertyName("clientRequestId")]
    public string ClientRequestId { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("eTag")]
    public string ETag { get; set; } = string.Empty;

    [JsonPropertyName("sequencer")]
    public string Sequencer { get; set; } = string.Empty;

    [JsonPropertyName("storageDiagnostics")]
    public StorageDiagnostics? StorageDiagnostics { get; set; }
}

public class StorageDiagnostics
{
    [JsonPropertyName("batchId")]
    public string BatchId { get; set; } = string.Empty;
}
