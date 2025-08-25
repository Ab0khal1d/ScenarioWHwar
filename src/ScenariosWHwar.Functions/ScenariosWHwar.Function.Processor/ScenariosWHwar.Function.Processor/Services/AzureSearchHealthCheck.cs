using Microsoft.Extensions.Diagnostics.HealthChecks;
using ScenariosWHwar.Function.Processor.Services;

namespace ScenariosWHwar.Function.Processor.Services;

/// <summary>
/// Health check for Azure Cognitive Search service
/// </summary>
public class AzureSearchHealthCheck : IHealthCheck
{
    private readonly IAzureSearchService _searchService;

    public AzureSearchHealthCheck(IAzureSearchService searchService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _searchService.IsHealthyAsync(cancellationToken);

            return isHealthy
                ? HealthCheckResult.Healthy("Azure Cognitive Search is healthy")
                : HealthCheckResult.Unhealthy("Azure Cognitive Search is not responding");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Azure Cognitive Search health check failed", ex);
        }
    }
}
