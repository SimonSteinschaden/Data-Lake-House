using Enset.Api.Logging;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Coordination;
using Enset.Application.Imports.Resolution;
using Enset.Application.Imports.WriteGate;
using Enset.Infrastructure.Imports.Analysis;
using Enset.Infrastructure.Imports.Database;
using Enset.Infrastructure.Imports.Excel;
using Enset.Infrastructure.Imports.Persistence;
using Enset.Infrastructure.Imports.RawZone;
using Enset.Infrastructure.Imports.Validation;
using Enset.Application.Authorization;

namespace Enset.Api.Extensions;

public static class ImportServiceCollectionExtensions
{
    public static IServiceCollection AddImportServices(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var appDataPath = Path.Combine(
            environment.ContentRootPath,
            "App_Data");

        var reportPath = Path.Combine(
            appDataPath,
            "import-reports");

        var stagingPath = Path.Combine(
            appDataPath,
            "staging");

        var rawZonePath = Path.Combine(
            appDataPath,
            "raw-zone");

        var outputPath = Path.Combine(
            appDataPath,
            "outputs");

        services.AddSingleton<IImportLogger, ApiImportLogger>();

        services.AddSingleton<IImportReportRepository>(
            new JsonImportReportRepository(reportPath));

        services.AddScoped<IImportReferenceValidationService,
            EfImportReferenceValidationService>();

        services.AddScoped<IImportAnalysisService>(serviceProvider =>
            new ExcelImportAnalysisService(
                stagingPath,
                serviceProvider.GetRequiredService<IImportReportRepository>(),
                serviceProvider.GetRequiredService<IImportLogger>(),
                serviceProvider.GetRequiredService<IImportReferenceValidationService>(),
                serviceProvider.GetRequiredService<ICurrentUserContext>()));

        services.AddSingleton<
            IApplyResolutionService,
            ApplyResolutionService>();

        services.AddSingleton<
            IImportWriteGate,
            ImportWriteGate>();

        services.AddSingleton<IImportWriter>(
            new ExcelImportWriter(outputPath));

        services.AddScoped<
            IImportWriter,
            DatabaseImportWriter>();

        services.AddSingleton<IRawZoneWriter>(
            new FileSystemRawZoneWriter(rawZonePath));

        services.AddScoped<
            IImportCommitService,
            ImportCommitService>();

        return services;
    }
}
/*
Verantwortung

AnalysisService

Repository

CommitService

WriteGate

RawZone

Writer
*/
