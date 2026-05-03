using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Enset.Infrastructure.Persistence;

public class EnsetDbContextFactory : IDesignTimeDbContextFactory<EnsetDbContext>
{
    public EnsetDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EnsetDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=enset_datalakehouse;Username=postgres;Password=postgres"
        );

        return new EnsetDbContext(optionsBuilder.Options);
    }
}