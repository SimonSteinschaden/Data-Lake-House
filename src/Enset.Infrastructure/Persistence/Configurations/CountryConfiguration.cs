using Enset.Domain.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsoCode2).HasMaxLength(2).IsRequired();
        builder.Property(x => x.IsoCode3).HasMaxLength(3);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => x.IsoCode2).IsUnique();
        builder.HasIndex(x => x.IsoCode3).IsUnique();
    }
}
