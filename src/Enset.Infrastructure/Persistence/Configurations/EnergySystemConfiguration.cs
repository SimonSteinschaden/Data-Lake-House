using Enset.Domain.Energy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class EnergySystemConfiguration : IEntityTypeConfiguration<EnergySystem>
{
    public void Configure(EntityTypeBuilder<EnergySystem> builder)
    {
        builder.ToTable("EnergySystems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EnergySystemNumber)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ExternalIdentifier).HasMaxLength(256);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(64);

        builder.HasIndex(x => x.EnergySystemNumber).IsUnique();

        builder.HasOne(x => x.Address)
            .WithMany(x => x.EnergySystems)
            .HasForeignKey(x => x.AddressId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
