using Enset.Domain.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class PostalCodeAreaConfiguration
    : IEntityTypeConfiguration<PostalCodeArea>
{
    public void Configure(EntityTypeBuilder<PostalCodeArea> builder)
    {
        builder.ToTable("PostalCodeAreas");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128);

        builder.HasIndex(x => new { x.CountryId, x.Code }).IsUnique();

        builder.HasOne(x => x.Country)
            .WithMany(x => x.PostalCodeAreas)
            .HasForeignKey(x => x.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Municipalities)
            .WithMany(x => x.PostalCodeAreas)
            .UsingEntity<Dictionary<string, object>>(
                "MunicipalityPostalCodeAreas",
                right => right
                    .HasOne<Municipality>()
                    .WithMany()
                    .HasForeignKey("MunicipalityId")
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<PostalCodeArea>()
                    .WithMany()
                    .HasForeignKey("PostalCodeAreaId")
                    .OnDelete(DeleteBehavior.Cascade));
    }
}
