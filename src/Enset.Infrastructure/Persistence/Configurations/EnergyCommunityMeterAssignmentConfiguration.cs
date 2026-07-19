using Enset.Domain.EnergyCommunities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class EnergyCommunityMeterAssignmentConfiguration
    : IEntityTypeConfiguration<EnergyCommunityMeterAssignment>
{
    public void Configure(
        EntityTypeBuilder<EnergyCommunityMeterAssignment> builder)
    {
        builder.ToTable("EnergyCommunityMeterAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.Property(x => x.AllocationShare)
            .HasPrecision(9, 6);

        builder.HasOne(x => x.EnergyCommunity)
            .WithMany(x => x.MeterAssignments)
            .HasForeignKey(x => x.EnergyCommunityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Meter)
            .WithMany(x => x.EnergyCommunityAssignments)
            .HasForeignKey(x => x.MeterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.EnergyCommunityId,
            x.MeterId,
            x.ValidFrom
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.MeterId,
            x.IsActive
        });
    }
}