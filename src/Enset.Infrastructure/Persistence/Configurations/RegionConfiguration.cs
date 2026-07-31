using Enset.Domain.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> builder)
    {
        builder.ToTable("Regions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(64);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => new { x.CountryId, x.Code }).IsUnique();

        builder.HasOne(x => x.Country)
            .WithMany(x => x.Regions)
            .HasForeignKey(x => x.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Municipalities)
            .WithMany(x => x.Regions)
            .UsingEntity<Dictionary<string, object>>(
                "RegionMunicipalities",
                right => right
                    .HasOne<Municipality>()
                    .WithMany()
                    .HasForeignKey("MunicipalityId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<Region>()
                    .WithMany()
                    .HasForeignKey("RegionId")
                    .OnDelete(DeleteBehavior.Cascade));
    }
}
