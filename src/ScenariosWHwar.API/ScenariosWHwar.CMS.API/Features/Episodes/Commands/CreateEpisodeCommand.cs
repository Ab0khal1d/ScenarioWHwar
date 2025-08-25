using ScenariosWHwar.API.Core.Host.Extensions;

namespace ScenariosWHwar.CMS.API.Features.Episodes.Commands;

public enum AllowedExtension
{
    MP4,
    MP3
}

public static class CreateEpisodeCommand
{
    public record Request(
        string Title,
        string Description,
        string Category,
        AllowedExtension AllowedExtension,
        string Language,
        float Duration) : IRequest<ErrorOr<EpisodeCreationResponseDto>>;

    public record EpisodeCreationResponseDto(
        EpisodeResponseDto Episode,
        string SasUrl);

    public record EpisodeResponseDto(
        int Id,
        string Title,
        string Description,
        string Category,
        string Language,
        float Duration,
        DateTime PublishDate,
        string Status,
        string? BlobPath,
        string SourceType,
        DateTime CreatedAt,
        DateTime UpdatedAt);


    public class Endpoint : IEndpoint
    {
        public static void MapEndpoint(IEndpointRouteBuilder endpoints)
        {
            endpoints
                .MapApiGroup(EpisodesFeature.FeatureName)
                .MapPost("/",
                async (ISender sender, Request request, CancellationToken ct) =>
                {
                    var result = await sender.Send(request, ct);
                    return result.Match(
                        success => TypedResults.Created($"/api/admin/episodes/{success.Episode.Id}", success),
                        CustomResult.Problem);
                })
                .WithName("CreateEpisode")
                .ProducesPost<EpisodeCreationResponseDto>();
        }
    }

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
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

            RuleFor(x => x.Language)
                .NotEmpty()
                .WithErrorCode(EpisodeErrors.Validation.LanguageRequired.Code)
                .WithMessage(EpisodeErrors.Validation.LanguageRequired.Description)
                .MaximumLength(10);

            RuleFor(x => x.Duration)
                .GreaterThanOrEqualTo(0);
        }

        private bool BeValidCategory(string category)
        {
            return Enum.TryParse<EpisodeCategory>(category, true, out _);
        }
    }

    internal sealed class Handler : IRequestHandler<Request, ErrorOr<EpisodeCreationResponseDto>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUploadBlobStorageService _blobStorageService;

        public Handler(ApplicationDbContext dbContext, IUploadBlobStorageService blobStorageService)
        {
            _dbContext = dbContext;
            _blobStorageService = blobStorageService;
        }

        public async Task<ErrorOr<EpisodeCreationResponseDto>> Handle(
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
                EpisodeFormat.From(request.AllowedExtension.ToString().ToLower(System.Globalization.CultureInfo.CurrentCulture)),
                request.Language,
                url: string.Empty,
                SourceType.DirectUpload,
                request.Duration);

            // Save to database
            await _dbContext.Episodes.AddAsync(episode, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Generate blob path and SAS URL
            var blobPath = episode.GenerateBlobPath();

            // Generate SAS URL for upload
            var sasUrl = await _blobStorageService.GenerateUploadSasUrlAsync(blobPath, cancellationToken);

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
            var response = new EpisodeCreationResponseDto(episodeDto, sasUrl);

            return response;
        }
    }
}
