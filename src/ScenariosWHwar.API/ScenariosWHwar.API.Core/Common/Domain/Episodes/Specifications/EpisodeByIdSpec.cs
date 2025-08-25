namespace ScenariosWHwar.API.Core.Common.Domain.Episodes.Specifications;

// For more on the Specification Pattern see: https://www.ssw.com.au/rules/use-specification-pattern/
public sealed class EpisodeByIdSpec : SingleResultSpecification<Episode>
{
    public EpisodeByIdSpec(EpisodeId episodeId)
    {
        Query.Where(e => e.Id == episodeId);
    }

    public EpisodeByIdSpec(int episodeId) : this(EpisodeId.From(episodeId))
    {
    }
}
