using Enset.Domain.Buildings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class BuildingVersionConfiguration
    : IEntityTypeConfiguration<BuildingVersion>
{
    public void Configure(EntityTypeBuilder<BuildingVersion> builder)
    {
        builder.ToTable("BuildingVersions");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Building)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.BuildingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.BuildingId,
            x.VersionNumber
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.BuildingId,
            x.ValidFrom
        });

        builder.Property(x => x.ChangeReason)
            .HasMaxLength(512);

        builder.HasOne(x => x.Address)
            .WithMany(x => x.BuildingVersions)
            .HasForeignKey(x => x.AddressId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(x => x.CadastralMunicipality)
            .HasMaxLength(256);

        builder.Property(x => x.PropertyNumber)
            .HasMaxLength(128);

        builder.Property(x => x.BuildingRegistryIdentifier)
            .HasMaxLength(128);

        builder.Property(x => x.PrimaryUseType)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(x => x.BuildingCategory)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(x => x.OwnershipType)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(x => x.GrossFloorAreaM2)
            .HasPrecision(18, 2);

        builder.Property(x => x.NetFloorAreaM2)
            .HasPrecision(18, 2);

        builder.Property(x => x.ConditionedFloorAreaM2)
            .HasPrecision(18, 2);

        builder.Property(x => x.HeatedFloorAreaM2)
            .HasPrecision(18, 2);

        builder.Property(x => x.CooledFloorAreaM2)
            .HasPrecision(18, 2);

        builder.Property(x => x.BuildingVolumeM3)
            .HasPrecision(18, 2);
    }
}
