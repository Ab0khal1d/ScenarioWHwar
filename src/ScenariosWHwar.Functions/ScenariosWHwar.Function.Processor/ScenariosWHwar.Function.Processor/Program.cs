using Azure.Search.Documents;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using ScenariosWHwar.Function.Processor.Configuration;
using ScenariosWHwar.Function.Processor.Data;
using ScenariosWHwar.Function.Processor.Services;
using ScenariosWHwar.CMS.API.Common.Persistence;
using ScenariosWHwar.API.Core.Common.Configurations;


var builder = FunctionsApplication.CreateBuilder(args);


// Add configuration
builder.Services.Configure<AzureSearchConfig>(
    builder.Configuration.GetSection(AzureSearchConfig.SectionName));
builder.Services.Configure<AzureStorageConfig>(
    builder.Configuration.GetSection(AzureStorageConfig.SectionName));
builder.Services.Configure<ProcessorConfig>(
    builder.Configuration.GetSection(ProcessorConfig.SectionName));

//// Add Entity Framework
//builder.Services.AddDbContext<ProcessorDbContext>(options =>
//{
//    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//    options.UseSqlServer(connectionString, sqlOptions =>
//    {
//        sqlOptions.EnableRetryOnFailure(
//            maxRetryCount: 3,
//            maxRetryDelay: TimeSpan.FromSeconds(5),
//            errorNumbersToAdd: null);
//    });

//    // Enable sensitive data logging in development
//    if (builder.Environment.IsDevelopment())
//    {
//        options.EnableSensitiveDataLogging();
//        options.EnableDetailedErrors();
//    }
//});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                         "Server=(localdb)\\mssqllocaldb;Database=ScenariosWHwar_CMS;Trusted_Connection=true;MultipleActiveResultSets=true";

    options.UseSqlServer(connectionString);
});

// Add Azure Search client
builder.Services.AddSingleton<SearchClient>(serviceProvider =>
{
    var config = builder.Configuration.GetSection(AzureSearchConfig.SectionName).Get<AzureSearchConfig>()
        ?? throw new InvalidOperationException("Azure Search configuration is missing");

    var searchUri = new Uri(config.ServiceEndpoint);
    var credential = new Azure.AzureKeyCredential(config.AdminApiKey);

    return new SearchClient(searchUri, config.IndexName, credential);
});

// Add application services
builder.Services.AddScoped<IEpisodeRepository, EpisodeRepository>();
builder.Services.AddScoped<IAzureSearchService, AzureSearchService>();
builder.Services.AddScoped<IEpisodeProcessorService, EpisodeProcessorService>();
builder.Services.AddScoped<IRetryPolicyService, RetryPolicyService>();

builder.Build().Run();
