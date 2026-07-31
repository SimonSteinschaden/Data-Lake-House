using Enset.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public sealed class UserCustomerAssignmentConfiguration
    : IEntityTypeConfiguration<UserCustomerAssignment>
{
    public void Configure(EntityTypeBuilder<UserCustomerAssignment> builder)
    {
        builder.ToTable("UserCustomerAssignments");
        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Role)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.HasOne(assignment => assignment.User)
            .WithMany(user => user.CustomerAssignments)
            .HasForeignKey(assignment => assignment.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(assignment => assignment.Customer)
            .WithMany(customer => customer.UserAssignments)
            .HasForeignKey(assignment => assignment.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(assignment => assignment.UserId);
        builder.HasIndex(assignment => assignment.CustomerId);
        builder.HasIndex(assignment => assignment.Role);
        builder.HasIndex(assignment => assignment.IsActive);
        builder.HasIndex(assignment => new { assignment.UserId, assignment.CustomerId })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");
    }
}
