using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.DuplicationCheck.Abstractions;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Issues;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Reports;

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

    public ImportCoordinator(
        IImportReader reader,
        IImportMapper mapper,
        IImportValidator validator,
        IDuplicationCheckService duplicationCheckService,
        IImportLogger logger)
    {
        _reader = reader;
        _mapper = mapper;
        _validator = validator;
        _duplicationCheckService = duplicationCheckService;
        _logger = logger;
    }

    public Task<ImportReport> RunAsync(CancellationToken cancellationToken = default)
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

        var report = _validator.Validate(customers, buildings, meters, meterReadings);
        report.Customers = customerDtos;
        report.Buildings = buildingDtos;
        report.Meters = meterDtos;
        report.MeterReadings = meterReadingDtos;

        _logger.Info($"Validation finished with {report.Issues.Count} issue(s).");

        var duplicateIssues = _duplicationCheckService
            .DetectCustomers(customerDtos)
            .ToList();

        report.Issues.AddRange(duplicateIssues);
        report.Decision = ImportDecisionEngine.Decide(report);
        report.Status = report.Issues.Any(issue =>
                (issue.RequiresUserDecision && !issue.IsResolved) ||
                (issue.Severity >= ImportIssueSeverity.Error && !issue.IsResolved))
            ? ImportStatus.AwaitingResolution
            : ImportStatus.ReadyToCommit;
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

        return Task.FromResult(report);
    }
}
