namespace ScenariosWHwar.API.Core.Common.Domain.Episodes.Specifications;

// For more on the Specification Pattern see: https://www.ssw.com.au/rules/use-specification-pattern/
public sealed class EpisodesCountSpec : Specification<Episode>
{
    public EpisodesCountSpec(
        EpisodeStatus? status = null,
        EpisodeCategory? category = null,
        string? searchTerm = null)
    {
        // Apply status filter
        if (status.HasValue)
        {
            Query.Where(e => e.Status == status.Value);
        }

        // Apply category filter
        if (category.HasValue)
        {
            Query.Where(e => e.Category == category.Value);
        }

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim().ToLowerInvariant();
            Query.Where(e => e.Title.Contains(normalizedSearchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                           e.Description.Contains(normalizedSearchTerm, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
