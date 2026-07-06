using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.DuplicationCheck.Abstractions;
using Enset.Application.Imports.WriteGate;


namespace Enset.Application.Imports.Coordination;
/// <summary>
/// Coordinates the import process by orchestrating the reading, mapping, validation, duplication check, and writing of import data.
/// Reader → Mapper → Validator → DuplicationCheck → WriteGate → Writer
/// </summary>
public sealed class ImportCoordinator : IImportCoordinator
{
    private readonly IImportReader _reader;
    private readonly IImportMapper _mapper;
    private readonly IImportValidator _validator;
    private readonly IDuplicationCheckService _duplicationCheckService;
    private readonly IImportWriteGate _writeGate;
    private readonly IImportWriter _writer;
    private readonly IImportLogger _logger;

    public ImportCoordinator(
        IImportReader reader,
        IImportMapper mapper,
        IImportValidator validator,
        IDuplicationCheckService duplicationCheckService,
        IImportWriteGate writeGate,
        IImportWriter writer,
        IImportLogger logger)
    {
        _reader = reader;
        _mapper = mapper;
        _validator = validator;
        _duplicationCheckService = duplicationCheckService;
        _writeGate = writeGate;
        _writer = writer;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.Info("Import started.");

        var workbook = _reader.Read();

        var customers = workbook.Customers.ToList();
        var buildings = workbook.Buildings.ToList();

        _logger.Info($"Read {customers.Count} customer row(s).");
        _logger.Info($"Read {buildings.Count} building row(s).");

        var report = _validator.Validate(customers, buildings);
        var customerDtos = _mapper.Map(customers);

        _logger.Info($"Validation finished with {report.Issues.Count} issue(s).");

        var duplicateIssues = _duplicationCheckService
            .DetectCustomers(customerDtos)
            .ToList();

        report.Issues.AddRange(duplicateIssues);

        _logger.Info($"Duplication check finished with {duplicateIssues.Count} issue(s).");

        var issueGroups = report.Issues
            .GroupBy(issue => new { issue.Type, issue.Severity })
            .OrderByDescending(group => group.Count());

        foreach (var group in issueGroups)
        {
            _logger.Warning(
                $"{group.Key.Type} | {group.Key.Severity} | {group.Count()} issue(s)");
        }

        var writeContext = new ImportWriteContext
        {
            Decision = ImportDecisionEngine.Decide(report),
            UserConfirmed = true,
            Issues = report.Issues,
            Customers = customerDtos
        };

        if (!_writeGate.CanWrite(writeContext))
        {
            _logger.Warning("WriteGate blocked import.");

            foreach (var issue in report.Issues.Take(20))
            {
                _logger.Warning(
                    $"{issue.Type} | {issue.Severity} | {issue.Message}");
            }

            if (report.Issues.Count > 20)
            {
                _logger.Warning($"... {report.Issues.Count - 20} further issue(s) not shown.");
            }

            return Task.CompletedTask;
        }

        _logger.Info("WriteGate approved import.");

        _writer.Write(writeContext);

        _logger.Info("Import finished successfully.");
        
        return Task.CompletedTask;
    }
}
