using Enset.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public sealed class EntityAuditEntryConfiguration : IEntityTypeConfiguration<EntityAuditEntry>
{
    public void Configure(EntityTypeBuilder<EntityAuditEntry> builder)
    {
        builder.ToTable("EntityAuditHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.FieldName).HasMaxLength(256);
        builder.Property(x => x.ChangeType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.OldValue).HasMaxLength(4000);
        builder.Property(x => x.NewValue).HasMaxLength(4000);
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.ChangedAtUtc });
        builder.HasIndex(x => x.ChangedByUserId);
    }
}
