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
using Enset.Application.Imports.MassImport;
using Enset.Infrastructure.Imports.MassImport;
using Enset.Application.Reporting;
using Enset.Infrastructure.Reporting;

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
        var reportInstancePath = Path.Combine(
            appDataPath,
            "report-instances");

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
                serviceProvider.GetRequiredService<ICurrentUserContext>(),
                serviceProvider.GetRequiredService<
                    IMeterReadingStreamingAnalyzer>(),
                serviceProvider.GetRequiredService<
                    Microsoft.Extensions.Options.IOptions<
                        MeterReadingMassImportOptions>>()));

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

        services.AddOptions<MeterReadingMassImportOptions>();
        services.AddSingleton<IMeterReadingStreamingReader,
            MeterReadingStreamingReader>();
        services.AddSingleton<IMeterReadingStreamingAnalyzer,
            MeterReadingStreamingAnalyzer>();
        services.AddScoped<IMeterReadingMassImportProcessor,
            MeterReadingMassImportProcessor>();
        services.AddSingleton<MeterReadingMassImportBackgroundService>();
        services.AddSingleton<IMeterReadingMassImportJobs>(sp =>
            sp.GetRequiredService<
                MeterReadingMassImportBackgroundService>());
        services.AddHostedService(sp =>
            sp.GetRequiredService<
                MeterReadingMassImportBackgroundService>());
        services.AddScoped<IReportService>(sp =>
            new FileReportService(
                reportInstancePath,
                sp.GetRequiredService<
                    Enset.Application.ObjectAnalytics
                        .IObjectAnalyticsService>(),
                sp.GetRequiredService<
                    Enset.Application.Quality
                        .IHierarchicalQualityAssessmentService>(),
                sp.GetRequiredService<TimeProvider>()));

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
