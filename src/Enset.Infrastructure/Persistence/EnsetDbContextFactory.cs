using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Enset.Infrastructure.Persistence;

public class EnsetDbContextFactory
    : IDesignTimeDbContextFactory<EnsetDbContext>
{
    private const string ConnectionStringEnvironmentVariable =
        "ENSET_CONNECTION_STRING";

    private const string DevelopmentConnectionString =
        "Host=localhost;Port=5432;Database=enset_datalakehouse;Username=postgres;Password=postgres";

    public EnsetDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = DevelopmentConnectionString;
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<EnsetDbContext>();

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(
                typeof(EnsetDbContext).Assembly.FullName));

        return new EnsetDbContext(optionsBuilder.Options);
    }
}