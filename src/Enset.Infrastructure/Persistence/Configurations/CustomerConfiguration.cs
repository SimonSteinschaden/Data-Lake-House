using Enset.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerNumber)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => x.CustomerNumber)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.LegalName)
            .HasMaxLength(256);

        builder.Property(x => x.CompanyRegistrationNumber)
            .HasMaxLength(64);

        builder.Property(x => x.VatIdentificationNumber)
            .HasMaxLength(64);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.Phone)
            .HasMaxLength(64);

        builder.Property(x => x.Website)
            .HasMaxLength(512);

        builder.Property(x => x.Street)
            .HasMaxLength(256);

        builder.Property(x => x.HouseNumber)
            .HasMaxLength(32);

        builder.Property(x => x.PostalCode)
            .HasMaxLength(32);

        builder.Property(x => x.City)
            .HasMaxLength(128);

        builder.Property(x => x.CountryCode)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(64);
    }
}