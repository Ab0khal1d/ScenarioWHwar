namespace ScenariosWHwar.API.Core.Common.Domain.Episodes.Specifications;

// For more on the Specification Pattern see: https://www.ssw.com.au/rules/use-specification-pattern/
public sealed class EpisodesBySourceTypeSpec : Specification<Episode>
{
    public EpisodesBySourceTypeSpec(SourceType sourceType)
    {
        Query.Where(e => e.SourceType == sourceType)
             .OrderByDescending(e => e.CreatedAt);
    }
}
