using Enset.Infrastructure.Imports.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Imports.Persistence.Configurations;

public sealed class ImportReportEntityConfiguration
    : IEntityTypeConfiguration<ImportReportEntity>
{
    public void Configure(EntityTypeBuilder<ImportReportEntity> builder)
    {
        builder.ToTable("ImportReports");

        builder.HasKey(report => report.ImportId);

        builder.Property(report => report.Status)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(report => report.SourceFileName)
            .HasMaxLength(512);

        builder.Property(report => report.SourceFileContentType)
            .HasMaxLength(128);

        builder.Property(report => report.SourceFileSha256)
            .HasMaxLength(128);

        builder.Property(report => report.SourceFileStagedPath)
            .HasMaxLength(1024);

        builder.Property(report => report.SourceFileRawStoragePath)
            .HasMaxLength(1024);

        builder.HasMany(report => report.Issues)
            .WithOne(issue => issue.ImportReport)
            .HasForeignKey(issue => issue.ImportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(report => report.AuditTrail)
            .WithOne(audit => audit.ImportReport)
            .HasForeignKey(audit => audit.ImportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
