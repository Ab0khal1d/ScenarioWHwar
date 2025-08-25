namespace ScenariosWHwar.API.Core.Common.Domain.Episodes.Specifications;

// For more on the Specification Pattern see: https://www.ssw.com.au/rules/use-specification-pattern/
public sealed class EpisodesAdvancedSearchSpec : Specification<Episode>
{
    public EpisodesAdvancedSearchSpec(
        EpisodeStatus[]? statuses = null,
        EpisodeCategory[]? categories = null,
        string[]? languages = null,
        SourceType[]? sourceTypes = null,
        DateTime? publishedAfter = null,
        DateTime? publishedBefore = null,
        float? minDuration = null,
        float? maxDuration = null,
        string? searchTerm = null)
    {
        // Apply multiple status filters
        if (statuses?.Any() == true)
        {
            Query.Where(e => statuses.Contains(e.Status));
        }

        // Apply multiple category filters
        if (categories?.Any() == true)
        {
            Query.Where(e => categories.Contains(e.Category));
        }

        // Apply language filters
        if (languages?.Any() == true)
        {
            Query.Where(e => languages.Contains(e.Language));
        }

        // Apply source type filters
        if (sourceTypes?.Any() == true)
        {
            Query.Where(e => sourceTypes.Contains(e.SourceType));
        }

        // Apply publish date range filters
        if (publishedAfter.HasValue)
        {
            Query.Where(e => e.PublishDate >= publishedAfter.Value);
        }

        if (publishedBefore.HasValue)
        {
            Query.Where(e => e.PublishDate <= publishedBefore.Value);
        }

        // Apply duration range filters
        if (minDuration.HasValue)
        {
            Query.Where(e => e.Duration >= minDuration.Value);
        }

        if (maxDuration.HasValue)
        {
            Query.Where(e => e.Duration <= maxDuration.Value);
        }

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim().ToLowerInvariant();
            Query.Where(e => e.Title.Contains(normalizedSearchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                           e.Description.Contains(normalizedSearchTerm, StringComparison.CurrentCultureIgnoreCase));
        }

        // Default ordering
        Query.OrderByDescending(e => e.PublishDate)
             .ThenByDescending(e => e.CreatedAt);
    }
}
