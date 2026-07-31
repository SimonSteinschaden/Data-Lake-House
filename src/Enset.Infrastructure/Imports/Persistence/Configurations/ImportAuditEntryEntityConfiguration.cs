using Enset.Infrastructure.Imports.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Imports.Persistence.Configurations;

public sealed class ImportAuditEntryEntityConfiguration
    : IEntityTypeConfiguration<ImportAuditEntryEntity>
{
    public void Configure(EntityTypeBuilder<ImportAuditEntryEntity> builder)
    {
        builder.ToTable("ImportAuditEntries");

        builder.HasKey(audit => audit.AuditId);

        builder.HasIndex(audit => audit.ImportId);

        builder.HasIndex(audit => audit.IssueId);

        builder.Property(audit => audit.UserId)
            .HasMaxLength(256);

        builder.Property(audit => audit.Action)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(audit => audit.PreviousResolutionAction)
            .HasMaxLength(64);

        builder.Property(audit => audit.ResolutionAction)
            .HasMaxLength(64);

        builder.Property(audit => audit.PreviousCustomResolvedValue)
            .HasMaxLength(4000);

        builder.Property(audit => audit.CustomResolvedValue)
            .HasMaxLength(4000);

        builder.Property(audit => audit.Details)
            .HasMaxLength(4000);
    }
}
