using Enset.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.ExternalIdentity).HasMaxLength(256).IsRequired();
        builder.HasIndex(user => user.ExternalIdentity).IsUnique();
        builder.Property(user => user.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.GlobalRole)
            .HasConversion<string>()
            .HasMaxLength(64);
    }
}
