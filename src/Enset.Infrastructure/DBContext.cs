using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MeterReading>()
            .HasKey(x => new { x.MeterId, x.Timestamp });

        modelBuilder.Entity<MeterReading>()
            .HasIndex(x => x.Timestamp);

        modelBuilder.Entity<MeterReading>()
            .HasIndex(x => new { x.MeterId, x.Timestamp });
    }

    protected override void Up(MigrationBuilder migrationBuilder)
    {
    // bestehender Code (Tables etc.)

    migrationBuilder.Sql(@"
        SELECT create_hypertable(
            'MeterReadings',
            'Timestamp',
            if_not_exists => TRUE
        );
    ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    migrationBuilder.Sql(@"
        DROP TABLE IF EXISTS ""MeterReadings"" CASCADE;
    ");
    }
}