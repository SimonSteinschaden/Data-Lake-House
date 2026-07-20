using Enset.Domain.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Street).HasMaxLength(256);
        builder.Property(x => x.HouseNumber).HasMaxLength(32);
        builder.Property(x => x.AddressAddition).HasMaxLength(128);
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);

        builder.HasOne(x => x.Country)
            .WithMany(x => x.Addresses)
            .HasForeignKey(x => x.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Municipality)
            .WithMany(x => x.Addresses)
            .HasForeignKey(x => x.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PostalCodeArea)
            .WithMany(x => x.Addresses)
            .HasForeignKey(x => x.PostalCodeAreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
