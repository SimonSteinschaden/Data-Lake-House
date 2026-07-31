using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.DuplicationCheck.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Coordination;

/// <summary>
/// Analyzes import data without applying user decisions or writing data.
/// Reader -> Mapper -> Validator -> DuplicationCheck -> ImportReport
/// </summary>
public sealed class ImportCoordinator : IImportCoordinator
{
    private readonly IImportReader _reader;
    private readonly IImportMapper _mapper;
    private readonly IImportValidator _validator;
    private readonly IDuplicationCheckService _duplicationCheckService;
    private readonly IImportLogger _logger;
    private readonly IImportReferenceValidationService? _referenceValidationService;

    public ImportCoordinator(
        IImportReader reader,
        IImportMapper mapper,
        IImportValidator validator,
        IDuplicationCheckService duplicationCheckService,
        IImportLogger logger,
        IImportReferenceValidationService? referenceValidationService = null)
    {
        _reader = reader;
        _mapper = mapper;
        _validator = validator;
        _duplicationCheckService = duplicationCheckService;
        _logger = logger;
        _referenceValidationService = referenceValidationService;
    }

    public async Task<ImportReport> RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Import analysis started.");
        cancellationToken.ThrowIfCancellationRequested();

        var workbook = _reader.Read();
        var customers = workbook.Customers.ToList();
        var buildings = workbook.Buildings.ToList();
        var meters = workbook.Meters.ToList();
        var meterReadings = workbook.MeterReadings.ToList();

        _logger.Info($"Read {customers.Count} customer row(s).");
        _logger.Info($"Read {buildings.Count} building row(s).");
        _logger.Info($"Read {meters.Count} meter row(s).");
        _logger.Info($"Read {meterReadings.Count} meter reading row(s).");

        var customerDtos = _mapper.Map(customers);
        var buildingDtos = buildings.Select(BuildingExcelRowMapper.ToDto).ToList();
        var meterDtos = meters.Select(MeterExcelRowMapper.ToDto).ToList();
        var meterReadingDtos = meterReadings.Select(MeterReadingExcelRowMapper.ToDto).ToList();
        _logger.Info("Mapping finished.");
        cancellationToken.ThrowIfCancellationRequested();

        var report = _validator.Validate(
            customers,
            buildings,
            meters,
            meterReadings,
            workbook.SourceType);
        report.SourceType = workbook.SourceType;
        report.Customers = customerDtos;
        report.Buildings = buildingDtos;
        report.Meters = meterDtos;
        report.MeterReadings = meterReadingDtos;
        report.CsvMapping = workbook.CsvMapping;
        if (workbook.CsvMapping is not null)
        {
            AddCsvColumnSelectionIssues(
                report,
                workbook.CsvMapping,
                meterReadings);
        }

        if (_referenceValidationService is not null)
        {
            var referenceIssues = await _referenceValidationService.ValidateAsync(
                workbook,
                cancellationToken);
            report.Issues.AddRange(referenceIssues);
            _logger.Info(
                $"Reference validation finished with {referenceIssues.Count} issue(s).");
        }

        _logger.Info($"Validation finished with {report.Issues.Count} issue(s).");

        var duplicateIssues = _duplicationCheckService
            .DetectCustomers(customerDtos)
            .ToList();

        report.Issues.AddRange(duplicateIssues);
        report.RecalculateCommitReadiness();
        report.UpdatedAt = DateTime.UtcNow;

        _logger.Info($"Duplication check finished with {duplicateIssues.Count} issue(s).");

        var issueGroups = report.Issues
            .GroupBy(issue => new { issue.Type, issue.Severity })
            .OrderByDescending(group => group.Count());

        foreach (var group in issueGroups)
        {
            _logger.Warning(
                $"{group.Key.Type} | {group.Key.Severity} | {group.Count()} issue(s)");
        }

        _logger.Info("Import analysis finished.");

        return report;
    }

    private static void AddCsvColumnSelectionIssues(
        ImportReport report,
        CsvMeterReadingMapping mapping,
        IReadOnlyList<MeterReadingExcelRow> readings)
    {
        var headers = System.Text.Json.JsonSerializer.Serialize(
            mapping.DetectedHeaders);
        if (mapping.TimestampColumn is null)
        {
            report.Issues.Add(new ImportIssue
            {
                Type = ImportIssueType.TimestampColumnSelectionRequired,
                Severity = ImportIssueSeverity.Error,
                RequiresUserDecision = true,
                FieldName = "TimestampColumn",
                SecondValue = headers,
                Message = "Timestamp-Spalte ist nicht eindeutig. Bitte eine " +
                    "Quellspalte wählen oder Startzeit und Intervall angeben."
            });
        }
        if (mapping.ValueColumn is null)
        {
            report.Issues.Add(new ImportIssue
            {
                Type = ImportIssueType.ValueColumnSelectionRequired,
                Severity = ImportIssueSeverity.Error,
                RequiresUserDecision = true,
                FieldName = "ValueColumn",
                SecondValue = headers,
                Message = "Wertespalte ist nicht eindeutig. Bitte eine " +
                    "Quellspalte wählen."
            });
        }
        var hasDefaultMeter = readings.Any(reading =>
            !string.IsNullOrWhiteSpace(reading.DefaultMeterNumber));
        if (mapping.MeterNumberColumn is null && !hasDefaultMeter)
        {
            report.Issues.Add(new ImportIssue
            {
                Type = ImportIssueType.AssignMeterRequired,
                Severity = ImportIssueSeverity.Error,
                RequiresUserDecision = true,
                FieldName = "MeterId",
                Message =
                    "Für dieses Lastprofil konnte kein Zähler zugeordnet " +
                    "werden. Bitte wählen Sie den Zielzähler aus."
            });
        }
    }
}
