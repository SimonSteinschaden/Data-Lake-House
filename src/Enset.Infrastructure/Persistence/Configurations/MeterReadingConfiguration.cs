using Enset.Domain.Energy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class MeterReadingConfiguration
    : IEntityTypeConfiguration<MeterReading>
{
    public void Configure(EntityTypeBuilder<MeterReading> builder)
    {
        builder.ToTable("MeterReadings");

        builder.HasKey(x => new
        {
            x.MeterId,
            x.Timestamp
        });

        builder.Property(x => x.Timestamp)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Value)
            .HasPrecision(20, 6);

        builder.Property(x => x.ReadingType)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(x => x.QualityFlag)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.HasOne(x => x.Meter)
            .WithMany(x => x.Readings)
            .HasForeignKey(x => x.MeterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Timestamp);

        builder.HasIndex(x => new
        {
            x.MeterId,
            x.Timestamp
        });
    }
}