using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using ScenariosWHwar.Discovery.API.Host.Extensions;
using ScenariosWHwar.API.Core.Host.Extensions;
using ScenariosWHwar.API.Core.Common.Interfaces;

namespace ScenariosWHwar.Discovery.API.Features.Episodes.Queries;

public static class GetEpisodeByIdQuery
{
    // Using the same DTO as search results since the endpoint specification shows the same response
    public record EpisodeSearchResultDto(
        int Id,
        string Title,
        string Description,
        string Category,
        string Language,
        float Duration,
        DateTime PublishDate,
        string SourceType,
        string Status,
        string SourceUrl,
        string VideoUrl);

    public record Request(int Id) : IRequest<ErrorOr<EpisodeSearchResultDto>>;

    public class Endpoint : IEndpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder endpoints)
        {
            endpoints
                .MapApiGroup(EpisodesFeature.FeatureName)
                .MapGet("/{id}",
                    async (ISender sender, int id, CancellationToken cancellationToken) =>
                    {
                        var request = new Request(id);
                        var result = await sender.Send(request, cancellationToken);
                        return result.Match(TypedResults.Ok, CustomResult.Problem);
                    })
                .WithName("GetEpisodeById")
                .ProducesGet<EpisodeSearchResultDto>()
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Episode ID must be greater than 0");
        }
    }

    internal sealed class Handler : IRequestHandler<Request, ErrorOr<EpisodeSearchResultDto>>
    {
        private readonly IEpisodeSearchService _searchService;
        private readonly IReadOnlyBlobStorageService _blobStorageService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<Handler> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public Handler(
            IEpisodeSearchService searchService,
            IReadOnlyBlobStorageService blobStorageService,
            IDistributedCache cache,
            ILogger<Handler> logger)
        {
            _searchService = searchService;
            _blobStorageService = blobStorageService;
            _cache = cache;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<ErrorOr<EpisodeSearchResultDto>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // Create cache key for this episode
            var cacheKey = $"episodes:details:{request.Id}";

            // Try to get cached result first
            var cachedResult = await GetCachedResultAsync(cacheKey, cancellationToken);
            if (cachedResult is not null)
            {
                _logger.LogDebug("Cache hit for episode: {EpisodeId}", request.Id);
                return cachedResult;
            }

            _logger.LogDebug("Cache miss for episode: {EpisodeId}", request.Id);

            // Get episode from Azure Search service
            var searchResult = await _searchService.GetEpisodeByIdAsync(request.Id, cancellationToken);

            if (searchResult.IsError)
            {
                return searchResult.Errors;
            }

            try
            {
                // Generate secure read URL for the video if available
                var videoUrl = await _blobStorageService.GenerateReadUrlAsync(searchResult.Value.BlobPath, cancellationToken);

                // Map search document to DTO
                var episode = new EpisodeSearchResultDto(
                    int.Parse(searchResult.Value.Id),
                    searchResult.Value.Title,
                    searchResult.Value.Description,
                    searchResult.Value.Category,
                    searchResult.Value.Language,
                    searchResult.Value.Duration,
                    searchResult.Value.PublishDate,
                    searchResult.Value.SourceType,
                    searchResult.Value.Status,
                    searchResult.Value.SourceUrl,
                    videoUrl);

                // Cache the result for future requests
                await CacheResultAsync(cacheKey, episode, cancellationToken);

                return episode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate video URL for episode {EpisodeId}. Using empty URL.", request.Id);

                // Return episode with empty video URL if URL generation fails
                var episode = new EpisodeSearchResultDto(
                    int.Parse(searchResult.Value.Id),
                    searchResult.Value.Title,
                    searchResult.Value.Description,
                    searchResult.Value.Category,
                    searchResult.Value.Language,
                    searchResult.Value.Duration,
                    searchResult.Value.PublishDate,
                    searchResult.Value.SourceType,
                    searchResult.Value.Status,
                    searchResult.Value.SourceUrl,
                    string.Empty);

                // Cache the result even with empty video URL
                await CacheResultAsync(cacheKey, episode, cancellationToken);

                return episode;
            }
        }

        private async Task<EpisodeSearchResultDto?> GetCachedResultAsync(
            string cacheKey,
            CancellationToken cancellationToken)
        {
            try
            {
                var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

                if (string.IsNullOrEmpty(cachedValue))
                    return null;

                return JsonSerializer.Deserialize<EpisodeSearchResultDto>(cachedValue, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve cached episode for key: {CacheKey}", cacheKey);
                return null;
            }
        }

        private async Task CacheResultAsync(
            string cacheKey,
            EpisodeSearchResultDto episode,
            CancellationToken cancellationToken)
        {
            try
            {
                var serializedEpisode = JsonSerializer.Serialize(episode, _jsonOptions);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1), // Cache individual episodes longer - 1 hour
                    SlidingExpiration = TimeSpan.FromMinutes(20) // Extend by 20 minutes on access
                };

                await _cache.SetStringAsync(cacheKey, serializedEpisode, cacheOptions, cancellationToken);

                _logger.LogDebug("Cached episode for key: {CacheKey}, expires in: {Expiration}",
                    cacheKey, cacheOptions.AbsoluteExpirationRelativeToNow);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache episode for key: {CacheKey}", cacheKey);
            }
        }
    }
}
