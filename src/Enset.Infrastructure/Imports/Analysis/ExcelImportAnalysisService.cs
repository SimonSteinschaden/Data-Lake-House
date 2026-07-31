using System.Security.Cryptography;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Coordination;
using Enset.Application.Imports.DuplicationCheck.Services;
using Enset.Application.Imports.Exceptions;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Validation;
using Enset.Infrastructure.Imports.Excel;
using Enset.Infrastructure.Imports.Readers;
using Enset.Application.Authorization;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Leb;
using Enset.Infrastructure.Imports.Leb;
using Enset.Infrastructure.Imports.MassImport;
using Microsoft.Extensions.Options;

namespace Enset.Infrastructure.Imports.Analysis;

public sealed class ExcelImportAnalysisService : IImportAnalysisService
{
    private readonly string _stagingPath;
    private readonly IImportReportRepository _reports;
    private readonly IImportLogger _logger;
    private readonly IImportReferenceValidationService _referenceValidationService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IMeterReadingStreamingAnalyzer _streamingAnalyzer;
    private readonly IOptions<MeterReadingMassImportOptions> _massOptions;

    public ExcelImportAnalysisService(
        string stagingPath,
        IImportReportRepository reports,
        IImportLogger logger,
        IImportReferenceValidationService referenceValidationService,
        ICurrentUserContext currentUser,
        IMeterReadingStreamingAnalyzer streamingAnalyzer,
        IOptions<MeterReadingMassImportOptions> massOptions)
    {
        _stagingPath = Path.GetFullPath(stagingPath);
        _reports = reports;
        _logger = logger;
        _referenceValidationService = referenceValidationService;
        _currentUser = currentUser;
        _streamingAnalyzer = streamingAnalyzer;
        _massOptions = massOptions;
        Directory.CreateDirectory(_stagingPath);
    }

    public async Task<ImportReport> AnalyzeAsync(
        Stream source,
        string fileName,
        string? contentType,
        string userId,
        CancellationToken cancellationToken = default,
        ImportSourceType sourceType = ImportSourceType.CRM_Excel,
        ImportMedium? medium = null,
        string? defaultMeterNumber = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("A source file name is required.", nameof(fileName));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A user id is required.", nameof(userId));

        var safeFileName = Path.GetFileName(fileName);
        _logger.Info($"File received: '{safeFileName}'.");
        var stagedPath = Path.Combine(
            _stagingPath,
            $"{Guid.NewGuid():N}_{safeFileName}");

        try
        {
            if (source.CanSeek)
                source.Position = 0;

            await using (var destination = new FileStream(
                stagedPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            var fileInfo = new FileInfo(stagedPath);
            _logger.Info($"File staged: '{safeFileName}', {fileInfo.Length} byte(s).");
            var sha256 = await CalculateSha256Async(stagedPath, cancellationToken);

            if (sourceType == ImportSourceType.Csv)
            {
                ValidateFileExtension(safeFileName, sourceType);
                var analysis = await _streamingAnalyzer.Analyze(
                    stagedPath,
                    defaultMeterNumber,
                    _massOptions.Value.ChunkSize,
                    cancellationToken);
                var csvReport = new ImportReport
                {
                    CreatedByUserId = _currentUser.UserId,
                    SourceType = ImportSourceType.Csv,
                    DefaultMeterNumber =
                        string.IsNullOrWhiteSpace(defaultMeterNumber)
                            ? null
                            : defaultMeterNumber.Trim(),
                    SourceFile = new ImportSourceFileMetadata
                    {
                        FileName = safeFileName,
                        ContentType = contentType,
                        Length = fileInfo.Length,
                        Sha256 = sha256,
                        StagedPath = stagedPath
                    },
                    MeterReadingAnalysis = analysis.Summary,
                    MeterReadings = analysis.Samples,
                    MeterReadingCount = checked(
                        (int)analysis.Summary.ReadRows),
                    Issues = analysis.Issues.ToList(),
                    UpdatedAt = DateTime.UtcNow
                };
                csvReport.RecalculateCommitReadiness();
                csvReport.AuditTrail.Add(new ImportAuditEntry
                {
                    Timestamp = csvReport.UpdatedAt,
                    UserId = userId,
                    Action = "StreamingAnalysisCompleted",
                    Details =
                        $"Source={safeFileName}; Rows=" +
                        $"{analysis.Summary.ReadRows}; Sha256={sha256}"
                });
                await _reports.SaveAsync(csvReport, cancellationToken);
                return csvReport;
            }

            var (reader, validator) = CreatePipeline(
                stagedPath,
                safeFileName,
                sourceType,
                medium,
                defaultMeterNumber);
            _logger.Info($"Selected import reader '{reader.GetType().Name}'.");

            var coordinator = new ImportCoordinator(
                reader,
                new CustomerImportMapper(),
                validator,
                new DuplicationCheckService(),
                _logger,
                _referenceValidationService);

            var report = await coordinator.RunAsync(cancellationToken);
            report.CreatedByUserId = _currentUser.UserId;
            report.DefaultMeterNumber =
                string.IsNullOrWhiteSpace(defaultMeterNumber)
                    ? null
                    : defaultMeterNumber.Trim();
            report.SourceFile = new ImportSourceFileMetadata
            {
                FileName = safeFileName,
                ContentType = contentType,
                Length = fileInfo.Length,
                Sha256 = sha256,
                StagedPath = stagedPath
            };
            report.UpdatedAt = DateTime.UtcNow;
            report.AuditTrail.Add(new ImportAuditEntry
            {
                Timestamp = report.UpdatedAt,
                UserId = userId,
                Action = "AnalysisCompleted",
                Details =
                    $"Source={safeFileName}; SourceType={sourceType}; " +
                    $"Medium={medium?.ToString() ?? "n/a"}; Sha256={sha256}"
            });

            await _reports.SaveAsync(report, cancellationToken);
            _logger.Info($"Import report '{report.ImportId}' saved.");
            return report;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.Error($"Import analysis failed for '{safeFileName}'.", exception);
            throw;
        }
    }

    private (IImportReader Reader, IImportValidator Validator) CreatePipeline(
        string stagedPath,
        string fileName,
        ImportSourceType sourceType,
        ImportMedium? medium,
        string? defaultMeterNumber)
    {
        ValidateFileExtension(fileName, sourceType);

        if (sourceType == ImportSourceType.Landesenergiebuchhaltung)
        {
            if (medium is null)
                throw new InvalidImportFileException(
                    "Für die Landesenergiebuchhaltung muss das Medium ausgewählt werden.");

            var sourceReader = new LebWorkbookReader();
            var sourceWorkbook = sourceReader.Read(stagedPath);
            return (
                new LebImportReader(sourceWorkbook, new LebWorkbookMapper(), medium.Value),
                new LebImportValidator(sourceWorkbook));
        }

        IImportReader reader = sourceType switch
        {
            ImportSourceType.CRM_Excel => new ExcelImportReader(
                new ExcelWorkbookReader(_logger),
                stagedPath),
            ImportSourceType.Csv => throw new InvalidOperationException(
                "CSV analysis must use the streaming analysis path."),
            _ => throw new InvalidImportFileException(
                $"Import source '{sourceType}' is not supported for this file.")
        };

        return (reader, new ExcelImportValidator());
    }

    public static void ValidateFileExtension(
        string fileName,
        ImportSourceType sourceType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (sourceType == ImportSourceType.CRM_Excel &&
            extension is not ".xlsx" and not ".xlsm")
        {
            throw new InvalidImportFileException(
                "CRM_Excel supports only .xlsx and .xlsm workbooks.");
        }

        if (sourceType == ImportSourceType.Csv && extension != ".csv")
        {
            throw new InvalidImportFileException(
                "Das Lastprofil erwartet eine CSV-Datei (*.csv).");
        }

        if (sourceType == ImportSourceType.Landesenergiebuchhaltung)
        {
            if (extension != ".csv")
                throw new InvalidImportFileException(
                    "Die Landesenergiebuchhaltung erwartet eine CSV-Datei (*.csv).");
        }
    }

    private static async Task<string> CalculateSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
