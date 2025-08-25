namespace ScenariosWHwar.API.Core.Common.Domain.Episodes.Specifications;

// For more on the Specification Pattern see: https://www.ssw.com.au/rules/use-specification-pattern/
public sealed class EpisodesByStatusSpec : Specification<Episode>
{
    public EpisodesByStatusSpec(EpisodeStatus status)
    {
        Query.Where(e => e.Status == status)
             .OrderByDescending(e => e.UpdatedAt);
    }
}
