using ScenariosWHwar.API.Core.Host.Extensions;

namespace ScenariosWHwar.CMS.API.Features.Episodes.Commands;

public static class DeleteEpisodeCommand
{
    public record Request(int Id) : IRequest<ErrorOr<Success>>;

    public class Endpoint : IEndpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder endpoints)
        {
            endpoints
                .MapApiGroup(EpisodesFeature.FeatureName)
                .MapDelete("/{episodeId:int}",
                    async (ISender sender, int episodeId, CancellationToken cancellationToken) =>
                    {
                        var result = await sender.Send(new Request(episodeId), cancellationToken);
                        return result.Match(_ => TypedResults.NoContent(), CustomResult.Problem);
                    })
                .WithName("DeleteEpisode")
                .ProducesDelete();
        }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0);
        }
    }

    internal sealed class Handler : IRequestHandler<Request, ErrorOr<Success>>
    {
        private readonly ApplicationDbContext _dbContext;

        public Handler(
            ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ErrorOr<Success>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // Find episode using specification
            var episode = await _dbContext.Episodes
                .WithSpecification(new EpisodeByIdSpec(request.Id))
                .FirstOrDefaultAsync(cancellationToken);

            if (episode is null)
                return EpisodeErrors.NotFound.EpisodeNotFound(request.Id);

            // Check if episode can be deleted
            if (episode.IsProcessing())
                return EpisodeErrors.Business.CannotDeleteProcessingEpisode;

            // Mark for deletion (raises domain event)
            episode.MarkForDeletion();

            // update status to Deleting
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new Success();
        }
    }

    // Message for service bus
    public class EpisodeDeleteMessage
    {
        public int EpisodeId { get; set; }
        public string? BlobPath { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
