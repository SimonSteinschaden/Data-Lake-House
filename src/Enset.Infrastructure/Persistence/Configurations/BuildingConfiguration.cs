using Enset.Domain.Buildings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("Buildings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BuildingNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => x.BuildingNumber)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ExternalIdentifier)
            .HasMaxLength(256);

        builder.HasOne(x => x.Project)
            .WithMany(x => x.Buildings)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.District)
            .WithMany()
            .HasForeignKey(x => x.DistrictId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}