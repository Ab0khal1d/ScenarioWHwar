using ScenariosWHwar.API.Core.Host.Extensions;

namespace ScenariosWHwar.CMS.API.Features.Episodes.Queries;

public static class GetImportJobStatusQuery
{
    public record Request(int EpisodeId) : IRequest<ErrorOr<CheckImportStatusResponseDto>>;

    public record CheckImportStatusResponseDto(string Status);
    public class Endpoint : IEndpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder endpoints)
        {
            endpoints
                .MapApiGroup(EpisodesFeature.FeatureName)
                .MapGet("/import/status/{episodeId:int}",
                    async (ISender sender, int episodeId, CancellationToken cancellationToken) =>
                    {
                        var request = new Request(episodeId);
                        var result = await sender.Send(request, cancellationToken);
                        return result.Match(TypedResults.Ok, CustomResult.Problem);
                    })
                .WithName("GetEpisodeImportStatus")
                .ProducesGet<CheckImportStatusResponseDto>();
        }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.EpisodeId)
                .NotEmpty()
                .WithMessage("Episode ID cannot be empty");
        }
    }

    internal sealed class Handler : IRequestHandler<Request, ErrorOr<CheckImportStatusResponseDto>>
    {
        private readonly ApplicationDbContext _dbContext;

        public Handler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ErrorOr<CheckImportStatusResponseDto>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            var episode = await _dbContext.Episodes
                 .WithSpecification(new EpisodeByIdSpec(request.EpisodeId))
                 .FirstOrDefaultAsync(cancellationToken);

            if (episode == null)
                return EpisodeErrors.NotFound.EpisodeNotFound(request.EpisodeId);

            return new CheckImportStatusResponseDto(episode.Status.ToString());
        }
    }
}
