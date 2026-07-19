using Enset.Domain.EnergyCommunities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class EnergyCommunityConfiguration
    : IEntityTypeConfiguration<EnergyCommunity>
{
    public void Configure(EntityTypeBuilder<EnergyCommunity> builder)
    {
        builder.ToTable("EnergyCommunities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CommunityNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => x.CommunityNumber)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(64);
    }
}