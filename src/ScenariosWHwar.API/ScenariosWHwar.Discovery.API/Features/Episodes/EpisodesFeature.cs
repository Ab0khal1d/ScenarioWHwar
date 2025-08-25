using ScenariosWHwar.Discovery.API.Common.Services;
using ScenariosWHwar.API.Core.Common.Interfaces;
using ScenariosWHwar.API.Core.Common.Configurations;

namespace ScenariosWHwar.Discovery.API.Features.Episodes;

public sealed class EpisodesFeature : IFeature
{
    public static string FeatureName => "Episodes";

    public static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // Configure Azure Search options
        services.Configure<AzureSearchConfig>(config.GetSection(AzureSearchConfig.SectionName));

        // Configure Azure Storage options
        services.Configure<AzureStorageConfig>(config.GetSection(AzureStorageConfig.SectionName));

        // Register search service
        services.AddScoped<IEpisodeSearchService, AzureSearchEpisodeService>();

        // Register read-only blob storage service
        services.AddScoped<IReadOnlyBlobStorageService, ReadOnlyBlobStorageService>();
    }
}
