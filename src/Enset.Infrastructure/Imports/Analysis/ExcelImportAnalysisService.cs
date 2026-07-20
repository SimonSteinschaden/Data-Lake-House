using System.Security.Cryptography;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Coordination;
using Enset.Application.Imports.DuplicationCheck.Services;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Validation;
using Enset.Infrastructure.Imports.Excel;

namespace Enset.Infrastructure.Imports.Analysis;

public sealed class ExcelImportAnalysisService : IImportAnalysisService
{
    private readonly string _stagingPath;
    private readonly IImportReportRepository _reports;
    private readonly IImportLogger _logger;

    public ExcelImportAnalysisService(
        string stagingPath,
        IImportReportRepository reports,
        IImportLogger logger)
    {
        _stagingPath = Path.GetFullPath(stagingPath);
        _reports = reports;
        _logger = logger;
        Directory.CreateDirectory(_stagingPath);
    }

    public async Task<ImportReport> AnalyzeAsync(
        Stream source,
        string fileName,
        string? contentType,
        string userId,
        CancellationToken cancellationToken = default)
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

            var coordinator = new ImportCoordinator(
                new ExcelImportReader(new ExcelWorkbookReader(_logger), stagedPath),
                new CustomerImportMapper(),
                new ExcelImportValidator(),
                new DuplicationCheckService(),
                _logger);

            var report = await coordinator.RunAsync(cancellationToken);
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
                Details = $"Source={safeFileName}; Sha256={sha256}"
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

    private static async Task<string> CalculateSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
