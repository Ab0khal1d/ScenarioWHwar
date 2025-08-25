using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace ScenariosWHwar.Importer.Function;

public class EpisodeImporterFunction
{
    private readonly ILogger<EpisodeImporterFunction> _logger;

    public EpisodeImporterFunction(ILogger<EpisodeImporterFunction> logger)
    {
        _logger = logger;
    }

    [Function("ImportEpisodeFromUrl")]
    public async Task<HttpResponseData> ImportEpisodeFromUrl(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "import/url")]
        HttpRequestData req,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Importing episode from URL");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var importRequest = JsonSerializer.Deserialize<UrlImportRequest>(requestBody);

            if (importRequest == null || string.IsNullOrEmpty(importRequest.SourceUrl))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid import request");
                return badResponse;
            }

            _logger.LogInformation("Starting import from URL: {SourceUrl}", importRequest.SourceUrl);

            // TODO: Implement actual import logic
            // 1. Validate URL and source type
            // 2. Download media file
            // 3. Extract metadata (title, description, duration, etc.)
            // 4. Upload to blob storage
            // 5. Create episode record in database
            // 6. Queue for processing

            var jobId = Guid.NewGuid();

            // Simulate import process
            await Task.Delay(100);

            var result = new ImportJobResult(
                jobId,
                "InProgress",
                importRequest.SourceUrl,
                null,
                DateTime.UtcNow,
                null);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteStringAsync(JsonSerializer.Serialize(result));

            _logger.LogInformation("Started import job {JobId} for URL {SourceUrl}",
                jobId, importRequest.SourceUrl);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing episode from URL");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    [Function("ImportEpisodeFromFeed")]
    public async Task ImportEpisodeFromFeed(
        [TimerTrigger("0 0 */6 * * *")] // Every 6 hours
        TimerInfo timer,
        FunctionContext context)
    {
        _logger.LogInformation("Starting scheduled episode import from RSS feeds");

        try
        {
            // TODO: Implement RSS feed import
            // 1. Fetch configured RSS feeds
            // 2. Parse feed entries
            // 3. Check for new episodes
            // 4. Queue new episodes for import
            // 5. Update feed last checked timestamp

            var feedUrls = new[]
            {
                "https://example.com/podcast/feed.rss",
                "https://another.example.com/episodes.xml"
            };

            foreach (var feedUrl in feedUrls)
            {
                _logger.LogInformation("Processing feed: {FeedUrl}", feedUrl);

                // Simulate feed processing
                await Task.Delay(200);

                _logger.LogInformation("Completed processing feed: {FeedUrl}", feedUrl);
            }

            _logger.LogInformation("Completed scheduled episode import from RSS feeds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled feed import");
            throw;
        }
    }

    [Function("GetImportJobStatus")]
    public async Task<HttpResponseData> GetImportJobStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "import/status/{jobId}")]
        HttpRequestData req,
        string jobId,
        FunctionContext executionContext)
    {
        _logger.LogInformation("Getting import job status for job: {JobId}", jobId);

        try
        {
            if (!Guid.TryParse(jobId, out var parsedJobId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid job ID format");
                return badResponse;
            }

            // TODO: Implement actual job status lookup from database/storage

            var jobStatus = new ImportJobResult(
                parsedJobId,
                "Completed",
                "https://example.com/video.mp4",
                123, // Episode ID
                DateTime.UtcNow.AddMinutes(-30),
                DateTime.UtcNow);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(JsonSerializer.Serialize(jobStatus));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting import job status for job: {JobId}", jobId);

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }
}

public record UrlImportRequest(
    string SourceUrl,
    string? Title = null,
    string? Description = null,
    Dictionary<string, object>? Metadata = null);

public record ImportJobResult(
    Guid JobId,
    string Status,
    string SourceUrl,
    int? EpisodeId,
    DateTime CreatedAt,
    DateTime? CompletedAt);
