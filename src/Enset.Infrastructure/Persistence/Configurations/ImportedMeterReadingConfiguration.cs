using Enset.Domain.Energy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public sealed class ImportedMeterReadingConfiguration
    : IEntityTypeConfiguration<ImportedMeterReading>
{
    public void Configure(EntityTypeBuilder<ImportedMeterReading> builder)
    {
        builder.ToTable("ImportedMeterReadings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MeterNumberRaw).HasMaxLength(256);
        builder.Property(x => x.TimestampRaw).HasMaxLength(256);
        builder.Property(x => x.ValueRaw).HasMaxLength(256);
        builder.Property(x => x.QualityRaw).HasMaxLength(256);
        builder.Property(x => x.SourceName).HasMaxLength(512);
        builder.Property(x => x.ParsingError).HasMaxLength(4000);
        builder.Property(x => x.Timestamp)
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.Value).HasPrecision(20, 6);

        builder.HasOne(x => x.Meter)
            .WithMany()
            .HasForeignKey(x => x.MeterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.ImportId);
        builder.HasIndex(x => x.MeterId);
        builder.HasIndex(x => new { x.ImportId, x.RowNumber });
    }
}
