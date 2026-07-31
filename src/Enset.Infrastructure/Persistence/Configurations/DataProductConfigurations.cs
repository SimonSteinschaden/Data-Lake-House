using Enset.Domain.DataProducts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enset.Infrastructure.Persistence.Configurations;

public sealed class DataProductDefinitionConfiguration : IEntityTypeConfiguration<DataProductDefinition>
{
    public void Configure(EntityTypeBuilder<DataProductDefinition> b)
    {
        b.ToTable("DataProductDefinitions"); b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(128).IsRequired(); b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasMaxLength(256).IsRequired();
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(64);
        b.Property(x => x.ResultType).HasConversion<string>().HasMaxLength(64);
    }
}

public sealed class DataProductConfiguration : IEntityTypeConfiguration<DataProduct>
{
    public void Configure(EntityTypeBuilder<DataProduct> b)
    {
        b.ToTable("DataProducts"); b.HasKey(x => x.Id);
        b.Property(x => x.ProductNumber).HasMaxLength(128).IsRequired(); b.HasIndex(x => x.ProductNumber).IsUnique();
        b.Property(x => x.Name).HasMaxLength(256).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
        b.HasOne(x => x.Definition).WithMany(x => x.DataProducts).HasForeignKey(x => x.DefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DataProductScopeAssignmentConfiguration : IEntityTypeConfiguration<DataProductScopeAssignment>
{
    public void Configure(EntityTypeBuilder<DataProductScopeAssignment> b)
    {
        b.ToTable("DataProductScopeAssignments"); b.HasKey(x => x.Id);
        b.Property(x => x.ScopeType).HasConversion<string>().HasMaxLength(64);
        b.HasOne(x => x.DataProduct).WithMany(x => x.ScopeAssignments).HasForeignKey(x => x.DataProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Meter).WithMany().HasForeignKey(x => x.MeterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Building).WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EnergySystem).WithMany().HasForeignKey(x => x.EnergySystemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Municipality).WithMany().HasForeignKey(x => x.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Region).WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EnergyCommunity).WithMany().HasForeignKey(x => x.EnergyCommunityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DataProductVersionConfiguration : IEntityTypeConfiguration<DataProductVersion>
{
    public void Configure(EntityTypeBuilder<DataProductVersion> b)
    {
        b.ToTable("DataProductVersions"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.DataProductId, x.VersionNumber }).IsUnique();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(64); b.Property(x => x.Quality).HasConversion<string>().HasMaxLength(64);
        b.HasOne(x => x.DataProduct).WithMany(x => x.Versions).HasForeignKey(x => x.DataProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.GenerationRun).WithMany(x => x.GeneratedVersions).HasForeignKey(x => x.GenerationRunId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class DataProductValueConfiguration : IEntityTypeConfiguration<DataProductValue>
{
    public void Configure(EntityTypeBuilder<DataProductValue> b)
    {
        b.ToTable("DataProductValues"); b.HasKey(x => x.Id); b.Property(x => x.Key).HasMaxLength(128).IsRequired();
        b.Property(x => x.NumericValue).HasPrecision(20, 6); b.Property(x => x.Unit).HasMaxLength(64); b.Property(x => x.Quality).HasConversion<string>().HasMaxLength(64);
        b.HasOne(x => x.DataProductVersion).WithMany(x => x.Values).HasForeignKey(x => x.DataProductVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DataProductGenerationRunConfiguration : IEntityTypeConfiguration<DataProductGenerationRun>
{
    public void Configure(EntityTypeBuilder<DataProductGenerationRun> b)
    {
        b.ToTable("DataProductGenerationRuns"); b.HasKey(x => x.Id); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
        b.Property(x => x.GeneratorName).HasMaxLength(256); b.Property(x => x.GeneratorVersion).HasMaxLength(64); b.Property(x => x.TriggeredBy).HasMaxLength(128);
    }
}
