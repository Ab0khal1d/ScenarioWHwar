using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScenariosWHwar.CMS.API.Common.Persistence.Episodes;

public class EpisodeConfiguration : AuditableConfiguration<Episode>
{
    public override void PostConfigure(EntityTypeBuilder<Episode> builder)
    {
        builder.ToTable("Episodes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        // builder.Property(e => e.BlobPath)
        //     .HasConversion(
        //         b => b != null ? b.Value.Value : null,
        //         v => v != null ? BlobPath.From(v).Value : null)
        //     .HasMaxLength(1000);

        // Configure enums
        builder.Property(e => e.Category)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue(EpisodeStatus.PendingUpload);

        builder.Property(e => e.SourceType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue(SourceType.DirectUpload);

        // Configure primitives
        builder.Property(e => e.Language)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("ar");

        builder.Property(e => e.Duration)
            .HasColumnType("float")
            .HasDefaultValue(0);

        builder.Property(e => e.PublishDate)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Format)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.SourceUrl)
            .HasMaxLength(500);

        builder.Property(e => e.BlobPath)
            .HasMaxLength(500)
            .HasComputedColumnSql("CONCAT([Id], '.', [Format])");



        // Indexes for performance No need for index as no queries are done on db directly
        //builder.HasIndex(e => e.Status);
        //builder.HasIndex(e => e.Category);
        //builder.HasIndex(e => e.Language);
        //builder.HasIndex(e => e.SourceType);
        //builder.HasIndex(e => e.Title);
        //builder.HasIndex(e => e.Format);

        // Ignore domain events (handled by interceptor)
        builder.Ignore(e => e.DomainEvents);
    }
}
