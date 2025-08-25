
// NOT IN REQUIREMENTS

//namespace ScenariosWHwar.CMS.API.Features.Episodes.Queries;

//public static class GetAllEpisodesQuery
//{
//    // DTOs
//    public record PaginatedResponse<T>(
//        IReadOnlyList<T> Items,
//        int TotalCount,
//        int Page,
//        int PageSize,
//        int TotalPages);

//    public record Request(
//        int Page = 1,
//        int PageSize = 20,
//        EpisodeStatus? Status = null,
//        EpisodeCategory? Category = null,
//        string? SearchTerm = null) : IRequest<ErrorOr<PaginatedResponse<EpisodeResponseDto>>>;

//    public class Endpoint : IEndpoint
//    {
//        public static void MapEndpoint(IEndpointRouteBuilder endpoints)
//        {
//            endpoints
//                .MapApiGroup(EpisodesFeature.FeatureName)
//                .MapGet($"/{EpisodesFeature.FeatureName.ToLowerInvariant()}",
//                    async (ISender sender,
//                       int page = 1,
//                       int pageSize = 20,
//                           string? status = null,
//                           string? category = null,
//                           string? searchTerm = null,
//                           CancellationToken ct = default) =>
//                    {
//                        EpisodeStatus? parsedStatus = null;
//                        if (!string.IsNullOrEmpty(status) && Enum.TryParse<EpisodeStatus>(status, true, out var s))
//                            parsedStatus = s;

//                        EpisodeCategory? parsedCategory = null;
//                        if (!string.IsNullOrEmpty(category) && Enum.TryParse<EpisodeCategory>(category, true, out var c))
//                            parsedCategory = c;

//                        var request = new Request(page, pageSize, parsedStatus, parsedCategory, searchTerm);
//                        var result = await sender.Send(request, ct);
//                        return result.Match(TypedResults.Ok, CustomResult.Problem);
//                    })
//                .WithName("GetEpisodes")
//                .ProducesGet<IReadOnlyList<PaginatedResponse<EpisodeResponseDto>>>();
//        }
//    }

//    public class Validator : AbstractValidator<Request>
//    {
//        public Validator()
//        {
//            RuleFor(x => x.Page)
//                .GreaterThan(0)
//                .WithMessage("Page must be greater than 0");

//            RuleFor(x => x.PageSize)
//                .GreaterThan(0)
//                .LessThanOrEqualTo(100)
//                .WithMessage("PageSize must be between 1 and 100");
//        }
//    }

//    internal sealed class Handler : IRequestHandler<Request, ErrorOr<PaginatedResponse<EpisodeResponseDto>>>
//    {
//        private readonly ApplicationDbContext _dbContext;

//        public Handler(ApplicationDbContext dbContext)
//        {
//            _dbContext = dbContext;
//        }

//        public async Task<ErrorOr<PaginatedResponse<EpisodeResponseDto>>> Handle(
//            Request request,
//            CancellationToken cancellationToken)
//        {
//            // Get total count using the count specification
//            var countSpec = new EpisodesCountSpec(
//                request.Status,
//                request.Category,
//                request.SearchTerm);

//            var totalCount = await _dbContext.Episodes
//                .WithSpecification(countSpec)
//                .CountAsync(cancellationToken);

//            // Get filtered and paginated results using the filtered specification
//            var filteredSpec = new EpisodesFilteredSpec(
//                request.Status,
//                request.Category,
//                request.SearchTerm,
//                request.Page,
//                request.PageSize);

//            var episodes = await _dbContext.Episodes
//                .WithSpecification(filteredSpec)
//                .Select(e => new EpisodeResponseDto(
//                    e.Id,
//                    e.Title,
//                    e.Description,
//                    e.Category.ToString(),
//                    e.Language,
//                    e.Duration,
//                    e.PublishDate,
//                    e.Status.ToString(),
//                    e.BlobPath,
//                    e.SourceType.ToString(),
//                    e.CreatedAt,
//                    e.UpdatedAt,
//                    e.HasVideo() && e.Status == EpisodeStatus.Ready ? e.BlobPath : null))
//                .ToListAsync(cancellationToken);

//            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

//            var result = new PaginatedResponse<EpisodeResponseDto>(
//                episodes,
//                totalCount,
//                request.Page,
//                request.PageSize,
//                totalPages);

//            return result;
//        }
//    }
//}
