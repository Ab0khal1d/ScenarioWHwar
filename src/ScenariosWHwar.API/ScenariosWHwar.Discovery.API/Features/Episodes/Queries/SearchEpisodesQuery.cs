using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using ScenariosWHwar.Discovery.API.Host.Extensions;
using ScenariosWHwar.API.Core.Host.Extensions;
using ScenariosWHwar.API.Core.Common.Interfaces;

namespace ScenariosWHwar.Discovery.API.Features.Episodes.Queries;

public static class SearchEpisodesQuery
{
    // DTOs based on models.instructions.md
    public record EpisodeSearchResultDto(
        int Id,
        string Title,
        string Description,
        string Category,
        string Status,
        string SourceType,
        string Language,
        float Duration,
        DateTime PublishDate,
        string SourceUrl,
        string VideoUrl);

    public record PaginatedSearchResultDto<T>(
        IReadOnlyList<T> Results,
        int TotalCount,
        int Page,
        int PageSize);

    public record Request(
        string? Query = null,
        string? Category = null,
        string? Language = null,
        int Page = 1,
        int PageSize = 20) : IRequest<ErrorOr<PaginatedSearchResultDto<EpisodeSearchResultDto>>>;

    public class Endpoint : IEndpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder endpoints)
        {
            endpoints
                .MapApiGroup(EpisodesFeature.FeatureName)
                .MapGet("/search",
                    async (ISender sender,
                           string? q,
                           string? category,
                           string? language,
                           int page = 1,
                           int pageSize = 20,
                           CancellationToken cancellationToken = default) =>
                    {
                        var request = new Request(q, category, language, page, pageSize);
                        var result = await sender.Send(request, cancellationToken);
                        return result.Match(TypedResults.Ok, CustomResult.Problem);
                    })
                .WithName("SearchEpisodes")
                .ProducesGet<PaginatedSearchResultDto<EpisodeSearchResultDto>>();
        }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(0)
                .WithMessage("Page must be greater than 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .LessThanOrEqualTo(100)
                .WithMessage("PageSize must be between 1 and 100");
        }
    }

    internal sealed class Handler : IRequestHandler<Request, ErrorOr<PaginatedSearchResultDto<EpisodeSearchResultDto>>>
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

        public async Task<ErrorOr<PaginatedSearchResultDto<EpisodeSearchResultDto>>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // Create cache key based on search parameters
            var cacheKey = CreateCacheKey(request);

            // Try to get cached result first
            var cachedResult = await GetCachedResultAsync(cacheKey, cancellationToken);
            if (cachedResult is not null)
            {
                _logger.LogDebug("Cache hit for search query: {CacheKey}", cacheKey);
                return cachedResult;
            }

            _logger.LogDebug("Cache miss for search query: {CacheKey}", cacheKey);

            // Create search filter from request
            var filter = new EpisodeSearchFilter(
                request.Query,
                request.Category,
                request.Language,
                request.Page,
                request.PageSize);

            // Execute search using Azure Search service
            var searchResult = await _searchService.SearchEpisodesAsync(filter, cancellationToken);

            if (searchResult.IsError)
            {
                return searchResult.Errors;
            }

            // Map search documents to DTOs with video URLs
            var episodes = new List<EpisodeSearchResultDto>();

            foreach (var doc in searchResult.Value.Results)
            {
                try
                {
                    // Generate secure read URL for the video if the document has VideoUrl
                    var videoUrl = await _blobStorageService.GenerateReadUrlAsync(doc.BlobPath, cancellationToken);

                    episodes.Add(new EpisodeSearchResultDto(
                        int.Parse(doc.Id),
                        doc.Title,
                        doc.Description,
                        doc.Category,
                        doc.Status,
                        doc.SourceType,
                        doc.Language,
                        doc.Duration,
                        doc.PublishDate,
                        doc.SourceUrl,
                        videoUrl));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate video URL for episode {EpisodeId}. Using empty URL.", doc.Id);

                    // Continue with empty video URL if URL generation fails
                    episodes.Add(new EpisodeSearchResultDto(
                       int.Parse(doc.Id),
                        doc.Title,
                        doc.Description,
                        doc.Category,
                        doc.Status,
                        doc.SourceType,
                        doc.Language,
                        doc.Duration,
                        doc.PublishDate,
                        doc.SourceUrl,
                        string.Empty));
                }
            }

            var result = new PaginatedSearchResultDto<EpisodeSearchResultDto>(
                episodes,
                searchResult.Value.TotalCount,
                searchResult.Value.Page,
                searchResult.Value.PageSize);

            // Cache the result for future requests
            await CacheResultAsync(cacheKey, result, cancellationToken);

            return result;
        }

        private static string CreateCacheKey(Request request)
        {
            var keyParts = new List<string> { "episodes", "search" };

            if (!string.IsNullOrEmpty(request.Query))
                keyParts.Add($"q:{request.Query.ToLowerInvariant()}");

            if (!string.IsNullOrEmpty(request.Category))
                keyParts.Add($"cat:{request.Category.ToLowerInvariant()}");

            if (!string.IsNullOrEmpty(request.Language))
                keyParts.Add($"lang:{request.Language.ToLowerInvariant()}");

            keyParts.Add($"page:{request.Page}");
            keyParts.Add($"size:{request.PageSize}");

            return string.Join(":", keyParts);
        }

        private async Task<PaginatedSearchResultDto<EpisodeSearchResultDto>?> GetCachedResultAsync(
            string cacheKey,
            CancellationToken cancellationToken)
        {
            try
            {
                var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);

                if (string.IsNullOrEmpty(cachedValue))
                    return null;

                return JsonSerializer.Deserialize<PaginatedSearchResultDto<EpisodeSearchResultDto>>(
                    cachedValue, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve cached search result for key: {CacheKey}", cacheKey);
                return null;
            }
        }

        private async Task CacheResultAsync(
            string cacheKey,
            PaginatedSearchResultDto<EpisodeSearchResultDto> result,
            CancellationToken cancellationToken)
        {
            try
            {
                var serializedResult = JsonSerializer.Serialize(result, _jsonOptions);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15), // Cache for 15 minutes
                    SlidingExpiration = TimeSpan.FromMinutes(5) // Extend by 5 minutes on access
                };

                await _cache.SetStringAsync(cacheKey, serializedResult, cacheOptions, cancellationToken);

                _logger.LogDebug("Cached search result for key: {CacheKey}, expires in: {Expiration}",
                    cacheKey, cacheOptions.AbsoluteExpirationRelativeToNow);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache search result for key: {CacheKey}", cacheKey);
            }
        }
    }
}
