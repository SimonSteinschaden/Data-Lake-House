using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.DuplicationCheck.Abstractions;
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

        _logger.Info($"Read {customers.Count} customer row(s).");
        _logger.Info($"Read {buildings.Count} building row(s).");

        var customerDtos = _mapper.Map(customers);
        cancellationToken.ThrowIfCancellationRequested();

        var report = _validator.Validate(customers, buildings);
        report.Customers = customerDtos;

        _logger.Info($"Validation finished with {report.Issues.Count} issue(s).");

        var duplicateIssues = _duplicationCheckService
            .DetectCustomers(customerDtos)
            .ToList();

        report.Issues.AddRange(duplicateIssues);
        report.Decision = ImportDecisionEngine.Decide(report);

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
