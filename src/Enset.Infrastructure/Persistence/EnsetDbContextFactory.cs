using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Enset.Infrastructure.Persistence;

public class EnsetDbContextFactory : IDesignTimeDbContextFactory<EnsetDbContext>
{
    private const string ConnectionStringEnvironmentVariable =
        "ENSET_CONNECTION_STRING";

    public EnsetDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set the '{ConnectionStringEnvironmentVariable}' environment variable " +
                "before running Entity Framework Core design-time commands.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<EnsetDbContext>();

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(
                typeof(EnsetDbContext).Assembly.FullName));

        return new EnsetDbContext(optionsBuilder.Options);
    }
}
