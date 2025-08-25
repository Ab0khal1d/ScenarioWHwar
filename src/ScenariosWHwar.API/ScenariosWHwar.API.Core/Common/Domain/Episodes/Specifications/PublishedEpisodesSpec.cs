namespace ScenariosWHwar.API.Core.Common.Domain.Episodes.Specifications;

// For more on the Specification Pattern see: https://www.ssw.com.au/rules/use-specification-pattern/
public sealed class PublishedEpisodesSpec : Specification<Episode>
{
    public PublishedEpisodesSpec()
    {
        Query.Where(e => e.Status == EpisodeStatus.Ready)
             .OrderByDescending(e => e.PublishDate);
    }
}
