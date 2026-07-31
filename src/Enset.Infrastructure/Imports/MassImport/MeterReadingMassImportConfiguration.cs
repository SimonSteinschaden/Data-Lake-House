using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Imports.MassImport;

public sealed class MeterReadingImportJobConfiguration
    : IEntityTypeConfiguration<MeterReadingImportJobEntity>
{
    public void Configure(
        EntityTypeBuilder<MeterReadingImportJobEntity> builder)
    {
        builder.ToTable("MeterReadingImportJobs");
        builder.HasKey(x => x.JobId);
        builder.HasIndex(x => x.ImportId).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.Status).HasMaxLength(32);
        builder.Property(x => x.Phase).HasMaxLength(32);
        builder.Property(x => x.TargetMode).HasMaxLength(16);
        builder.Property(x => x.ErrorCode).HasMaxLength(64);
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
    }
}

public sealed class ImportStagingMeterReadingConfiguration
    : IEntityTypeConfiguration<ImportStagingMeterReading>
{
    public void Configure(
        EntityTypeBuilder<ImportStagingMeterReading> builder)
    {
        builder.ToTable("ImportStagingMeterReadings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => x.ImportId);
        builder.HasIndex(x => new { x.ImportId, x.BatchNumber });
        builder.HasIndex(x => new { x.ImportId, x.ValidationStatus });
        builder.HasIndex(x => new { x.MeterId, x.Timestamp });
        builder.HasIndex(x => new { x.ImportId, x.MeterId, x.Timestamp });
        builder.HasIndex(x => new
        {
            x.ImportId,
            x.MeterId,
            x.Timestamp,
            x.SourceRowNumber
        });
        builder.Property(x => x.MeterNumberOriginal).HasMaxLength(256);
        builder.Property(x => x.Unit).HasMaxLength(32);
        builder.Property(x => x.QualityFlag).HasMaxLength(32);
        builder.Property(x => x.ReadingType).HasMaxLength(32);
        builder.Property(x => x.EnergyDirection).HasMaxLength(32);
        builder.Property(x => x.ValidationStatus).HasMaxLength(32);
        builder.Property(x => x.ValidationCode).HasMaxLength(64);
        builder.Property(x => x.ValidationMessage).HasMaxLength(2000);
        builder.Property(x => x.RawSourceHash).HasMaxLength(64);
    }
}

public sealed class MeterReadingImportAuditConfiguration
    : IEntityTypeConfiguration<MeterReadingImportAuditEntity>
{
    public void Configure(
        EntityTypeBuilder<MeterReadingImportAuditEntity> builder)
    {
        builder.ToTable("MeterReadingImportAudits");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ImportId);
        builder.HasIndex(x => new { x.ImportId, x.MeterId });
        builder.Property(x => x.Source).HasMaxLength(64);
        builder.Property(x => x.Status).HasMaxLength(32);
        builder.Property(x => x.FailureReason).HasMaxLength(4000);
    }
}
