using Enset.Domain.Geography;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class StateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.ToTable("States");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => new { x.CountryId, x.Code }).IsUnique();

        builder.HasOne(x => x.Country)
            .WithMany(x => x.States)
            .HasForeignKey(x => x.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
