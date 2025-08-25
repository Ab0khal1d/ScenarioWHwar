using ScenariosWHwar.API.Core.Common.Configurations;
using ScenariosWHwar.CMS.API.Common.Services;

namespace ScenariosWHwar.CMS.API.Features.Episodes;

public sealed class EpisodesFeature : IFeature
{
    public static string FeatureName => "Episodes";

    public static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // Register services
        services.AddScoped<IUploadBlobStorageService, UploadBlobStorageService>();
        services.AddScoped<IIntegrationEventsPublisher, AzureSeviceBusPublisher>();

        // Configure Azure Blob Storage
        services.Configure<AzureStorageConfig>(config.GetSection(AzureStorageConfig.SectionName));

        // Configure Azure Service Bus
        services.Configure<ServiceBusConfig>(config.GetSection(ServiceBusConfig.SectionName));
    }
}
