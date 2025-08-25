using ScenariosWHwar.API.Core.Host.Extensions;
using System.Text.Json.Serialization;
using static ScenariosWHwar.CMS.API.Features.Episodes.Commands.CreateEpisodeCommand;

namespace ScenariosWHwar.CMS.API.Features.Episodes.Commands;

public static class UpdateEpisodeCommand
{
    public record Request(
       string Title,
       string Description,
       string Category,
       DateTime PublishDate) : IRequest<ErrorOr<EpisodeResponseDto>>
    {
        [JsonIgnore]
        public int EpisodeId { get; set; }
    }

    public class Endpoint : IEndpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder endpoints)
        {
            endpoints
               .MapApiGroup(EpisodesFeature.FeatureName)
               .MapPut("/{episodeId:int}",
                   async (ISender sender, int episodeId, Request request, CancellationToken cancellationToken) =>
                   {
                       request.EpisodeId = episodeId;
                       var result = await sender.Send(request, cancellationToken);
                       return result.Match(_ => TypedResults.NoContent(), CustomResult.Problem);
                   })
               .WithName("UpdateEpisode")
               .ProducesPut();
        }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.EpisodeId)
                .GreaterThan(0);

            RuleFor(x => x.Title)
               .NotEmpty()
               .WithErrorCode(EpisodeErrors.Validation.TitleRequired.Code)
               .WithMessage(EpisodeErrors.Validation.TitleRequired.Description)
               .MaximumLength(500)
               .WithErrorCode(EpisodeErrors.Validation.TitleNotExceed500Char.Code)
               .WithMessage(EpisodeErrors.Validation.TitleNotExceed500Char.Description);

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithErrorCode(EpisodeErrors.Validation.DescriptionNotExceed2000Char.Code)
                .WithMessage(EpisodeErrors.Validation.DescriptionNotExceed2000Char.Description);

            RuleFor(x => x.Category)
                .NotEmpty()
                .Must(BeValidCategory)
                .WithErrorCode(EpisodeErrors.Validation.CategoryInvalid.Code)
                .WithMessage(EpisodeErrors.Validation.CategoryInvalid.Description);

            RuleFor(x => x.PublishDate)
                .GreaterThanOrEqualTo(DateTime.Today)
                .WithErrorCode(EpisodeErrors.Validation.PublishDateMustBeFuture.Code)
                .WithMessage(errorMessage: EpisodeErrors.Validation.PublishDateMustBeFuture.Description);

        }

        private bool BeValidCategory(string category)
        {
            return Enum.TryParse<EpisodeCategory>(category, true, out _);
        }
    }

    internal sealed class Handler : IRequestHandler<Request, ErrorOr<EpisodeResponseDto>>
    {
        private readonly ApplicationDbContext _dbContext;

        public Handler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ErrorOr<EpisodeResponseDto>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // Find episode using specification
            var episode = await _dbContext.Episodes
                .WithSpecification(new EpisodeByIdSpec(request.EpisodeId))
                .FirstOrDefaultAsync(cancellationToken);

            if (episode is null)
                return EpisodeErrors.NotFound.EpisodeNotFound(request.EpisodeId);

            // Check if episode can be updated
            if (episode.IsProcessing())
                return EpisodeErrors.Business.CannotUpdateProcessingEpisode;

            // Create value objects
            var titleResult = EpisodeTitle.From(request.Title);
            var descriptionResult = EpisodeDescription.From(request.Description ?? string.Empty);

            // Parse category
            if (!Enum.TryParse<EpisodeCategory>(request.Category, true, out var category))
                return Error.Validation("Episode.CategoryInvalid", "Invalid category value");

            // Update episode
            episode.UpdateMetadata(
                titleResult,
                descriptionResult,
                category,
                request.PublishDate);

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Prepare response
            var response = new EpisodeResponseDto(
                episode.Id,
                episode.Title,
                episode.Description,
                episode.Category.ToString(),
                episode.Language,
                episode.Duration,
                episode.PublishDate,
                episode.Status.ToString(),
                episode.BlobPath,
                episode.SourceType.ToString(),
                episode.CreatedAt,
                episode.UpdatedAt);

            return response;
        }
    }
}
