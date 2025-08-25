using EntityFramework.Exceptions.SqlServer;
using ScenariosWHwar.CMS.API.Common.Persistence.Interceptors;

namespace ScenariosWHwar.CMS.API.Common.Persistence;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddScoped<EntitySaveChangesInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddSingleton(TimeProvider.System);

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                                 "Server=(localdb)\\mssqllocaldb;Database=ScenariosWHwar_CMS;Trusted_Connection=true;MultipleActiveResultSets=true";

            options.UseSqlServer(connectionString);

            var serviceProvider = builder.Services.BuildServiceProvider();

            options.AddInterceptors(
                serviceProvider.GetRequiredService<EntitySaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<DispatchDomainEventsInterceptor>());


            // Return strongly typed useful exceptions
            options.UseExceptionProcessor();
        });
    }
}
