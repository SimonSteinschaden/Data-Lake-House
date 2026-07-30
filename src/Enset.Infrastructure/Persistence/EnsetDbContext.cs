using Microsoft.EntityFrameworkCore;

using Enset.Domain.Analytics;
using Enset.Domain.Buildings;
using Enset.Domain.Customers;
using Enset.Domain.Documents;
using Enset.Domain.Energy;
using Enset.Domain.EnergyCommunities;
using Enset.Domain.Geography;
using Enset.Domain.DataProducts;
using Enset.Domain.Projects;
using Enset.Domain.Users;
using Enset.Domain.Common;
using Enset.Domain.Curation;
using Enset.Domain.GoldProfiles;
using Enset.Application.Authorization;

using Enset.Infrastructure.Imports.Persistence.Entities;

namespace Enset.Infrastructure.Persistence;

public class EnsetDbContext : DbContext
{
    private readonly ICurrentUserContext? _currentUser;

    public EnsetDbContext(DbContextOptions<EnsetDbContext> options,
        ICurrentUserContext? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<UserCustomerAssignment> UserCustomerAssignments
        => Set<UserCustomerAssignment>();
    public DbSet<CustomerBuildingAssignment> CustomerBuildingAssignments
        => Set<CustomerBuildingAssignment>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<BuildingVersion> BuildingVersions => Set<BuildingVersion>();

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<State> States => Set<State>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<PostalCodeArea> PostalCodeAreas => Set<PostalCodeArea>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<DataProductDefinition> DataProductDefinitions => Set<DataProductDefinition>();
    public DbSet<DataProduct> DataProducts => Set<DataProduct>();
    public DbSet<DataProductScopeAssignment> DataProductScopeAssignments => Set<DataProductScopeAssignment>();
    public DbSet<DataProductVersion> DataProductVersions => Set<DataProductVersion>();
    public DbSet<DataProductValue> DataProductValues => Set<DataProductValue>();
    public DbSet<DataProductGenerationRun> DataProductGenerationRuns => Set<DataProductGenerationRun>();

    public DbSet<EnergySystem> EnergySystems => Set<EnergySystem>();
    public DbSet<EnergySystemBuildingAssignment> EnergySystemBuildingAssignments
        => Set<EnergySystemBuildingAssignment>();

    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<ImportedMeterReading> ImportedMeterReadings
        => Set<ImportedMeterReading>();

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
    public DbSet<EntityAuditEntry> EntityAuditEntries => Set<EntityAuditEntry>();
    public DbSet<CurationTask> CurationTasks => Set<CurationTask>();
    public DbSet<CurationDecision> CurationDecisions => Set<CurationDecision>();
    public DbSet<CuratedFieldValue> CuratedFieldValues => Set<CuratedFieldValue>();
    public DbSet<GoldProfileVersion> GoldProfileVersions => Set<GoldProfileVersion>();
    public DbSet<GoldProfileEvent> GoldProfileEvents => Set<GoldProfileEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(EnsetDbContext).Assembly);

        var crudEntityTypes = new HashSet<Type>
        {
            typeof(Customer), typeof(Building), typeof(Meter),
            typeof(EnergySystem), typeof(MeterReading)
        };
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(x => crudEntityTypes.Contains(x.ClrType)))
        {
            var entity = modelBuilder.Entity(entityType.ClrType);
            entity.HasQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
            entity.Property(nameof(BaseEntity.CreatedAt)).HasColumnName("CreatedAtUtc");
            entity.Property(nameof(BaseEntity.UpdatedAt)).HasColumnName("UpdatedAtUtc");
            entity.Property(nameof(BaseEntity.DataOrigin)).HasConversion<string>().HasMaxLength(32)
                .HasDefaultValue(DataOrigin.Imported);
            entity.Property(nameof(BaseEntity.LastModifiedSource)).HasConversion<string>().HasMaxLength(32)
                .HasDefaultValue(LastModifiedSource.Import);
            entity.Property(nameof(BaseEntity.RowVersion)).IsRowVersion().HasColumnName("xmin");
            entity.HasIndex(nameof(BaseEntity.IsDeleted));
            entity.HasIndex(nameof(BaseEntity.UpdatedAt));
            entity.HasIndex(nameof(BaseEntity.DataOrigin));
        }
        var milestoneProperties = new[]
        {
            nameof(BaseEntity.CreatedByUserId), nameof(BaseEntity.UpdatedByUserId),
            nameof(BaseEntity.DeletedAtUtc), nameof(BaseEntity.DeletedByUserId),
            nameof(BaseEntity.IsDeleted), nameof(BaseEntity.DataOrigin),
            nameof(BaseEntity.LastImportId), nameof(BaseEntity.LastModifiedSource),
            nameof(BaseEntity.RowVersion)
        };
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(x => typeof(BaseEntity).IsAssignableFrom(x.ClrType) &&
                                 !crudEntityTypes.Contains(x.ClrType)))
        {
            var entity = modelBuilder.Entity(entityType.ClrType);
            foreach (var property in milestoneProperties)
                entity.Ignore(property);
        }
    }

    private static System.Linq.Expressions.LambdaExpression BuildSoftDeleteFilter(Type type)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(type, "entity");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
        return System.Linq.Expressions.Expression.Lambda(
            System.Linq.Expressions.Expression.Not(property), parameter);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyChangeTracking();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyChangeTracking();
        return base.SaveChanges();
    }

    private void ApplyChangeTracking()
    {
        ChangeTracker.DetectChanges();
        var now = DateTime.UtcNow;
        var userId = _currentUser?.UserId ?? Guid.Empty;
        var auditEntries = new List<EntityAuditEntry>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified)
                     .ToList())
        {
            var entity = entry.Entity;
            EntityChangeType changeType;
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
                entity.CreatedByUserId = userId;
                changeType = EntityChangeType.Created;
            }
            else
            {
                entity.UpdatedAt = now;
                entity.UpdatedByUserId = userId;
                if (entity.LastModifiedSource == LastModifiedSource.User &&
                    entity.DataOrigin == DataOrigin.Imported)
                    entity.DataOrigin = DataOrigin.ImportedAndModified;

                var deletedProperty =
                    entry.Metadata.FindProperty(nameof(BaseEntity.IsDeleted));
                var wasDeleted = deletedProperty is null
                    ? entity.IsDeleted
                    : entry.Property(nameof(BaseEntity.IsDeleted))
                        .OriginalValue as bool? ?? false;
                if (!wasDeleted && entity.IsDeleted)
                    entity.DeletedByUserId = userId;
                changeType = !wasDeleted && entity.IsDeleted
                    ? EntityChangeType.SoftDeleted
                    : wasDeleted && !entity.IsDeleted
                        ? EntityChangeType.Restored
                        : EntityChangeType.Updated;
            }

            var changed = entry.State == EntityState.Added
                ? entry.Properties.Where(x => !x.Metadata.IsShadowProperty())
                : entry.Properties.Where(x => x.IsModified);
            foreach (var property in changed)
            {
                auditEntries.Add(new EntityAuditEntry
                {
                    EntityType = entity.GetType().Name,
                    EntityId = entity.Id,
                    ChangedAtUtc = now,
                    ChangedByUserId = userId,
                    ChangeType = changeType,
                    FieldName = property.Metadata.Name,
                    OldValue = entry.State == EntityState.Added ? null : Format(property.OriginalValue),
                    NewValue = Format(property.CurrentValue),
                    Source = entity.LastModifiedSource,
                    ImportId = entity.LastImportId
                });
            }
        }

        EntityAuditEntries.AddRange(auditEntries);
    }

    private static string? Format(object? value) => value switch
    {
        null => null,
        DateTime date => date.ToUniversalTime().ToString("O"),
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
    };
}
