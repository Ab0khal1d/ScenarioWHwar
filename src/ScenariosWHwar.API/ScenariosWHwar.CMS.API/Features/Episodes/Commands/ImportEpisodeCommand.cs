using ScenariosWHwar.API.Core.Host.Extensions;
using static ScenariosWHwar.CMS.API.Features.Episodes.Commands.CreateEpisodeCommand;

namespace ScenariosWHwar.CMS.API.Features.Episodes.Commands;

public static class ImportEpisodeCommand
{
    public enum SourceTypeForImport
    {
        Youtube = SourceType.YoutubeImport,
        RSS = SourceType.RssImport
    }
    public record Request(
    string Title,
    string Description,
    SourceTypeForImport SourceType,
    string SourceUrl,
    string Category,
    string Language,
    float Duration) : IRequest<ErrorOr<ImportJobStatusResponseDto>>;

    public record ImportJobStatusResponseDto(EpisodeResponseDto Episode);

    public class Endpoint : IEndpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder endpoints)
        {
            endpoints
                .MapApiGroup(EpisodesFeature.FeatureName)
                .MapPost($"/import",
                    async (ISender sender, Request command, CancellationToken ct) =>
                    {
                        var result = await sender.Send(command, ct);
                        return result.Match(TypedResults.Ok, CustomResult.Problem);
                    })
                .WithName("ImportEpisode")
                .ProducesPost<ImportJobStatusResponseDto>();
        }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.SourceType)
                .NotEmpty();

            RuleFor(x => x.SourceUrl)
                .NotEmpty()
                .Must(BeValidUrl)
                .WithMessage("Source URL must be a valid URL");
        }


        private bool BeValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }

    internal sealed class Handler : IRequestHandler<Request, ErrorOr<ImportJobStatusResponseDto>>
    {
        private readonly ApplicationDbContext _dbContext;

        public Handler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ErrorOr<ImportJobStatusResponseDto>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            var titleResult = EpisodeTitle.From(request.Title);
            var descriptionResult = EpisodeDescription.From(request.Description ?? string.Empty);

            // Parse category
            if (!Enum.TryParse<EpisodeCategory>(request.Category, true, out var category))
                return EpisodeErrors.Validation.CategoryInvalid;

            // Create episode aggregate
            var episode = Episode.Create(
                titleResult,
                descriptionResult,
                category,
                EpisodeFormat.From("mp4"),
                request.Language,
                url: request.SourceUrl,
                (SourceType)request.SourceType,
                request.Duration);

            // Add domain event to notify import processor
            episode.NotifyImportProcessor();

            // Save to database
            await _dbContext.Episodes.AddAsync(episode, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Generate blob path and SAS URL
            var blobPath = episode.GenerateBlobPath();
            episode.UpdateBlobPath(BlobPathValue.From(blobPath));

            // Prepare response
            var episodeDto = new EpisodeResponseDto(
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
            var response = new ImportJobStatusResponseDto(episodeDto);

            return response;
        }
    }
}
