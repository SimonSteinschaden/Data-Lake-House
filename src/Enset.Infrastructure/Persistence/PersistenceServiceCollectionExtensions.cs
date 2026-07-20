using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Generators;
using Enset.Application.DataProducts.Generation.Services;
using Enset.Infrastructure.DataProducts;

namespace Enset.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddDbPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "A PostgreSQL connection string is required.",
                nameof(connectionString));
        }

        var migrationsAssembly = typeof(EnsetDbContext).Assembly.GetName().Name;

        services.AddDbContext<EnsetDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(migrationsAssembly)));

        services.AddScoped<EfDataProductRepository>();
        services.AddScoped<IDataProductRepository>(sp => sp.GetRequiredService<EfDataProductRepository>());
        services.AddScoped<IDataProductGenerationRunRepository>(sp => sp.GetRequiredService<EfDataProductRepository>());
        services.AddScoped<EfDataProductReader>();
        services.AddScoped<IMeterReadingDataReader>(sp => sp.GetRequiredService<EfDataProductReader>());
        services.AddScoped<IBuildingDataReader>(sp => sp.GetRequiredService<EfDataProductReader>());
        services.AddScoped<IDataProductGenerator, MeterConsumptionSummaryGenerator>();
        services.AddScoped<IDataProductGenerator, BuildingEnergyProfileGenerator>();
        services.AddScoped<IDataProductGenerationAuthorizationService, DataProductGenerationAuthorizationService>();
        services.AddScoped<IDataProductGenerationAvailabilityService, DataProductGenerationAvailabilityService>();
        services.AddScoped<IDataProductGenerationService, DataProductGenerationService>();

        return services;
    }
}
