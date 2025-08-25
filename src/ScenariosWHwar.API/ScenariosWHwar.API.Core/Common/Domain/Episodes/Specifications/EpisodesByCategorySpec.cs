namespace ScenariosWHwar.API.Core.Common.Domain.Episodes.Specifications;

// For more on the Specification Pattern see: https://www.ssw.com.au/rules/use-specification-pattern/
public sealed class EpisodesByCategorySpec : Specification<Episode>
{
    public EpisodesByCategorySpec(EpisodeCategory category)
    {
        Query.Where(e => e.Category == category)
             .OrderByDescending(e => e.PublishDate);
    }
}
