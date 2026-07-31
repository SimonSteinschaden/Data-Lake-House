using Enset.Domain.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class MunicipalityConfiguration : IEntityTypeConfiguration<Municipality>
{
    public void Configure(EntityTypeBuilder<Municipality> builder)
    {
        builder.ToTable("Municipalities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(32);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => new { x.DistrictId, x.Name });

        builder.HasOne(x => x.District)
            .WithMany(x => x.Municipalities)
            .HasForeignKey(x => x.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
