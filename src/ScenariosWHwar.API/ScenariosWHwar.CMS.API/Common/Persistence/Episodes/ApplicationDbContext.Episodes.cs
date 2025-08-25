namespace ScenariosWHwar.CMS.API.Common.Persistence;

public partial class ApplicationDbContext
{
    public DbSet<Episode> Episodes => AggregateRootSet<Episode>();
}
