namespace ScenariosWHwar.API.Core.Common.Interfaces;

public interface IFeature
{
    static abstract string FeatureName { get; }
    static abstract void ConfigureServices(IServiceCollection services, IConfiguration config);
}
