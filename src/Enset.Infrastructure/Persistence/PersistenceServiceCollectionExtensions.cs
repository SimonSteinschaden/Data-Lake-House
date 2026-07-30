using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Enset.Application.DataProducts.Generation.Abstractions;
using Enset.Application.DataProducts.Generation.Generators;
using Enset.Application.DataProducts.Generation.Services;
using Enset.Infrastructure.DataProducts;
using Enset.Application.Authorization;
using Enset.Infrastructure.Authorization;
using Enset.Application.ReadModel;
using Enset.Infrastructure.ReadModel;
using Enset.Application.Analytics;
using Enset.Infrastructure.Analytics;
using Enset.Application.Crud;
using Enset.Infrastructure.Crud;
using Enset.Application.Curation;
using Enset.Infrastructure.Curation;
using Enset.Application.GoldProfiles;
using Enset.Infrastructure.GoldProfiles;
using Enset.Application.InternalDataProducts;
using Enset.Application.CanonicalSnapshots;
using Enset.Infrastructure.CanonicalSnapshots;
using Enset.Infrastructure.InternalDataProducts;
using Enset.Application.Exports.LEB.Abstractions;
using Enset.Application.Exports.LEB.Services;
using Enset.Application.Exports.LEB.Validation;
using Enset.Infrastructure.Exports.LEB;
using Enset.Application.ObjectAnalytics;
using Enset.Infrastructure.ObjectAnalytics;
using Enset.Application.DataProducts.Catalog;
using Enset.Application.QualityManagement;
using Enset.Infrastructure.QualityManagement;
using Enset.Application.Associations;
using Enset.Infrastructure.Associations;

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
        services.AddScoped<ICurrentUserResolver, EfCurrentUserResolver>();
        services.AddScoped<IDataAccessScope, EfDataAccessScope>();
        services.AddScoped<IEntityReadService, EfEntityReadService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAnalyticsDataProductService, EfAnalyticsDataProductService>();
        services.AddScoped<IObjectAnalyticsService,
            CanonicalObjectAnalyticsService>();
        services.AddScoped<IDataProductCatalogService,
            CanonicalDataProductCatalogService>();
        services.AddScoped<IDataQualityDashboardService,
            CanonicalDataQualityDashboardService>();
        services.AddScoped<IAssociationService, EfAssociationService>();
        services.AddScoped<IEntityCrudService, EfEntityCrudService>();
        services.AddScoped<ICurationService, EfCurationService>();
        services.AddScoped<IGoldProfileVersionService, GoldProfileVersionService>();
        services.AddScoped<IDataProductReadinessService, DataProductReadinessService>();
        services.AddScoped<EfInternalDataProductService>();
        services.AddScoped<ICanonicalSnapshotReader,
            EfCanonicalSnapshotReader>();
        services.AddScoped<IBuildingSummaryProductService>(sp =>
            sp.GetRequiredService<EfInternalDataProductService>());
        services.AddScoped<IMeterSummaryProductService>(sp =>
            sp.GetRequiredService<EfInternalDataProductService>());
        services.AddScoped<ICustomerSummaryProductService>(sp =>
            sp.GetRequiredService<EfInternalDataProductService>());
        services.AddScoped<IPortfolioSummaryProductService>(sp =>
            sp.GetRequiredService<EfInternalDataProductService>());
        services.AddScoped<IImportQualityProductService>(sp =>
            sp.GetRequiredService<EfInternalDataProductService>());
        services.AddScoped<INoeLebContractBuilder, EfNoeLebContractBuilder>();
        services.AddSingleton<LebExportValidator>();
        services.AddSingleton<ICsvLebExporter, CsvLebExporter>();
        services.AddSingleton<IExcelLebExporter, ExcelLebExporter>();
        services.AddScoped<ILebExportService, LebExportService>();

        return services;
    }
}
