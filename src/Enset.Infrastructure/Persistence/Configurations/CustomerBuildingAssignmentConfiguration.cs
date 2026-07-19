using Enset.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class CustomerBuildingAssignmentConfiguration
    : IEntityTypeConfiguration<CustomerBuildingAssignment>
{
    public void Configure(
        EntityTypeBuilder<CustomerBuildingAssignment> builder)
    {
        builder.ToTable("CustomerBuildingAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(64);

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.BuildingAssignments)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Building)
            .WithMany(x => x.CustomerAssignments)
            .HasForeignKey(x => x.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.CustomerId,
            x.BuildingId,
            x.Role,
            x.ValidFrom
        });
    }
}