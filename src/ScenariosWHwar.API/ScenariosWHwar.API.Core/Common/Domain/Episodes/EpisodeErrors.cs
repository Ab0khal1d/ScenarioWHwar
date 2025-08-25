namespace ScenariosWHwar.API.Core.Common.Domain.Episodes;

public static class EpisodeErrors
{
    public static class NotFound
    {
        public static Error EpisodeNotFound(int id)
        {
            return Error.NotFound(
            "Episode.NotFound",
            $"Episode with ID {id} was not found");
        }
    }

    public static class Validation
    {
        public static Error TitleRequired => Error.Validation(
            "Episode.TitleRequired",
            "Episode title is required");
        public static Error TitleNotExceed500Char => Error.Validation(
            "Episode.TitleNotExceed500Char",
            "Episode title cannot exceed 500 characters");
        public static Error DescriptionNotExceed2000Char => Error.Validation(
            "Episode.DescriptionNotExceed2000Char",
            "Episode description cannot exceed 2000 characters");

        public static Error DurationMustBePositive => Error.Validation(
            "Episode.DurationMustBePositive",
            "Episode duration must be greater than 0");

        public static Error PublishDateMustBeFuture => Error.Validation(
            "Episode.PublishDateMustBeFuture",
            "Publish date cannot be in the past for new episodes");

        public static Error LanguageRequired => Error.Validation(
            "Episode.LanguageRequired",
            "Language code is required");

        public static Error BlobPathNotExceed1000Char => Error.Validation(
            "Episode.BlobPathNotExceed1000Char",
            "Blob path cannot exceed 1000 characters");

        public static Error CategoryInvalid => Error.Validation(
            "Episode.CategoryInvalid",
            "Invalid category value");

        public static Error FormatInvalid => Error.Validation(
            "Episode.FormatInvalid",
            "Episode format must be either 'mp3' or 'mp4'");
    }

    public static class Business
    {
        public static Error CannotDeleteProcessingEpisode => Error.Validation(
            "Episode.CannotDeleteProcessing",
            "Cannot delete an episode that is currently being processed");

        public static Error CannotUpdateProcessingEpisode => Error.Validation(
            "Episode.CannotUpdateProcessing",
            "Cannot update an episode that is currently being processed");

        public static Error EpisodeAlreadyReady => Error.Validation(
            "Episode.AlreadyReady",
            "Episode is already in Ready status");

        public static Error InvalidStatusTransition(EpisodeStatus from, EpisodeStatus to)
        {
            return Error.Validation(
            "Episode.InvalidStatusTransition",
            $"Cannot transition from {from} to {to}");
        }

        public static Error CantUpateSameStatus => Error.Validation(
            "Episode.CantUpateSameStatus",
            "New status must be different from the current status");
        public static Error CannotUpdateEpisodeToDeleting => Error.Validation(
            "Episode.CannotUpdateEpisodeToDeleting",
            "Can not directly update episode status to deleting");
    }

    public static class Integration
    {
        public static Error BlobStorageError => Error.Failure(
            "Episode.BlobStorageError",
            "Failed to interact with blob storage");

        public static Error ServiceBusError => Error.Failure(
            "Episode.ServiceBusError",
            "Failed to publish message to service bus");

        public static Error ImportJobError => Error.Failure(
            "Episode.ImportJobError",
            "Failed to start import job");
    }
}
