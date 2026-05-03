public class EnsetDbContext : DbContext
{
    public EnsetDbContext(DbContextOptions<EnsetDbContext> options)
        : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Building> Buildings => Set<Building>();

    public DbSet<EnergySystem> EnergySystems => Set<EnergySystem>();
    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<DataSource> DataSources => Set<DataSource>();

    public DbSet<CalculationResult> CalculationResults => Set<CalculationResult>();
    public DbSet<BenchmarkDataset> BenchmarkDatasets => Set<BenchmarkDataset>();
}