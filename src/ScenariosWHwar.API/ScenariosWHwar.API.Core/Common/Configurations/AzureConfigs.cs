using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScenariosWHwar.API.Core.Common.Configurations;
/// <summary>
/// Configuration for Azure Cognitive Search service
/// </summary>
public class AzureSearchConfig
{
    public const string SectionName = "AzureSearch";

    public string ServiceEndpoint { get; set; } = string.Empty;
    public string AdminApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = "azuresql-index";
    public int RetryMaxAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
}
/// <summary>
/// Configuration for Azure Blob Storage service
/// </summary>
public class AzureStorageConfig
{
    public const string SectionName = "AzureStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string VideosContainerName { get; set; } = "";
    public string BaseUrl { get; set; } = string.Empty;
    public int SasTokenExpiryMinutes { get; set; } = 60;
}

public class ServiceBusConfig
{
    public const string SectionName = "ServiceBus";
    public string ConnectionString { get; set; } = string.Empty;
    public string ImportQueueName { get; set; } = "episode-import";
    public string ProcessorQueueName { get; set; } = "episode-processor";
}