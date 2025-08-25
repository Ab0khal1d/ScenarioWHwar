
// NOT IN REQUIREMENTS

//namespace ScenariosWHwar.CMS.API.Features.Episodes.Queries;

//public static class GetEpisodeByIdQuery
//{
//    public record Request(int Id) : IRequest<ErrorOr<EpisodeResponseDto>>;

//    public class Endpoint : IEndpoint
//    {
//        public void Map(WebApplication app)
//        {
//            app.MapGet("/admin/episodes/{id}",
//                async (ISender sender, int id, CancellationToken ct) =>
//                {
//                    var request = new Request(id);
//                    var result = await sender.Send(request, ct);
//                    return result.Match(TypedResults.Ok, CustomResult.Problem);
//                })
//                .WithName("GetEpisodeById")
//                .ProducesGet<EpisodeResponseDto>()
//                .ProducesProblem(StatusCodes.Status404NotFound);
//        }
//    }

//    public class Validator : AbstractValidator<Request>
//    {
//        public Validator()
//        {
//            RuleFor(x => x.Id)
//                .GreaterThan(0)
//                .WithMessage("Episode ID must be greater than 0");
//        }
//    }

//    internal sealed class Handler : IRequestHandler<Request, ErrorOr<EpisodeResponseDto>>
//    {
//        private readonly ApplicationDbContext _dbContext;

//        public Handler(ApplicationDbContext dbContext)
//        {
//            _dbContext = dbContext;
//        }

//        public async Task<ErrorOr<EpisodeResponseDto>> Handle(
//            Request request,
//            CancellationToken cancellationToken)
//        {
//            var episode = await _dbContext.Episodes
//                .WithSpecification(new EpisodeByIdSpec(request.Id))
//                .FirstOrDefaultAsync(cancellationToken);

//            if (episode == null)
//                return EpisodeErrors.NotFound.EpisodeNotFound(request.Id);

//            var response = new EpisodeResponseDto(
//                episode.Id,
//                episode.Title,
//                episode.Description,
//                episode.Category.ToString(),
//                episode.Language,
//                episode.Duration,
//                episode.PublishDate,
//                episode.Status.ToString(),
//                episode.BlobPath,
//                episode.SourceType.ToString(),
//                episode.CreatedAt,
//                episode.UpdatedAt,
//                episode.HasVideo() && episode.Status == EpisodeStatus.Ready ? episode.BlobPath : null);

//            return response;
//        }
//    }
//}
