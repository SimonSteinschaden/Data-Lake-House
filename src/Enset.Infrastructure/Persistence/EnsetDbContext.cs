using Microsoft.EntityFrameworkCore;

using Enset.Domain.Analytics;
using Enset.Domain.Buildings;
using Enset.Domain.Customers;
using Enset.Domain.Documents;
using Enset.Domain.Energy;
using Enset.Domain.EnergyCommunities;
using Enset.Domain.Projects;

using Enset.Infrastructure.Imports.Persistence.Entities;

namespace Enset.Infrastructure.Persistence;

public class EnsetDbContext : DbContext
{
    public EnsetDbContext(DbContextOptions<EnsetDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerBuildingAssignment> CustomerBuildingAssignments
        => Set<CustomerBuildingAssignment>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<BuildingVersion> BuildingVersions => Set<BuildingVersion>();

    public DbSet<EnergySystem> EnergySystems => Set<EnergySystem>();
    public DbSet<EnergySystemBuildingAssignment> EnergySystemBuildingAssignments
        => Set<EnergySystemBuildingAssignment>();

    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();

    public DbSet<EnergyCommunity> EnergyCommunities
        => Set<EnergyCommunity>();

    public DbSet<EnergyCommunityMeterAssignment>
        EnergyCommunityMeterAssignments
        => Set<EnergyCommunityMeterAssignment>();

    public DbSet<Document> Documents => Set<Document>();

    // public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    // public DbSet<DataSource> DataSources => Set<DataSource>();

    public DbSet<CalculationResult> CalculationResults
        => Set<CalculationResult>();

    public DbSet<BenchmarkDataset> BenchmarkDatasets
        => Set<BenchmarkDataset>();

    public DbSet<ImportReportEntity> ImportReports
        => Set<ImportReportEntity>();

    public DbSet<ImportIssueEntity> ImportIssues
        => Set<ImportIssueEntity>();

    public DbSet<ImportAuditEntryEntity> ImportAuditEntries
        => Set<ImportAuditEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(EnsetDbContext).Assembly);
    }
}