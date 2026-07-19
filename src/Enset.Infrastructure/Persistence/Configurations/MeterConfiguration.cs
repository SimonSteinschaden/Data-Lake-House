using Enset.Domain.Energy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class MeterConfiguration : IEntityTypeConfiguration<Meter>
{
    public void Configure(EntityTypeBuilder<Meter> builder)
    {
        builder.ToTable("Meters");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MeterNumber)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(x => x.MeterNumber)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.ExternalIdentifier)
            .HasMaxLength(256);

        builder.Property(x => x.Manufacturer)
            .HasMaxLength(128);

        builder.Property(x => x.Model)
            .HasMaxLength(128);

        builder.Property(x => x.SerialNumber)
            .HasMaxLength(128);

        builder.Property(x => x.CommunicationProtocol)
            .HasMaxLength(64);

        builder.Property(x => x.Medium)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(x => x.Quantity)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(x => x.Unit)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(x => x.Direction)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.HasOne(x => x.Building)
            .WithMany(x => x.Meters)
            .HasForeignKey(x => x.BuildingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.EnergySystem)
            .WithMany(x => x.Meters)
            .HasForeignKey(x => x.EnergySystemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}