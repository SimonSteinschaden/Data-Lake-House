using Enset.Domain.Curation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public sealed class CurationTaskConfiguration : IEntityTypeConfiguration<CurationTask>
{
    public void Configure(EntityTypeBuilder<CurationTask> builder)
    {
        builder.ToTable("CurationTasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityDisplayName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FieldName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.OriginalValue).HasMaxLength(512);
        builder.Property(x => x.SuggestedValue).HasMaxLength(512).IsRequired();
        builder.Property(x => x.CuratedValue).HasMaxLength(512);
        builder.Property(x => x.Reasoning).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.RowVersion).IsRowVersion().HasColumnName("xmin");
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.FieldName }).IsUnique();
    }
}

public sealed class CurationDecisionConfiguration : IEntityTypeConfiguration<CurationDecision>
{
    public void Configure(EntityTypeBuilder<CurationDecision> builder)
    {
        builder.ToTable("CurationDecisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Decision).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.OriginalValue).HasMaxLength(512);
        builder.Property(x => x.SuggestedValue).HasMaxLength(512).IsRequired();
        builder.Property(x => x.NewValue).HasMaxLength(512);
        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.HasOne(x => x.Task).WithMany(x => x.Decisions)
            .HasForeignKey(x => x.CurationTaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CurationTaskId, x.DecidedAtUtc });
    }
}
