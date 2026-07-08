using Enset.Application.Imports.Coordination;
using Enset.Application.Imports.DuplicationCheck.Services;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Validation;
using Enset.Infrastructure.Imports.Excel;
using Enset.Worker;
using Enset.Worker.Logging;

// Entwicklungsmodus
const string DevelopmentWorkbook =
    @"C:\Users\rdpadmin\Desktop\Repositories\Data Lake House\Data-Lake-House-main\Data-Lake-House\Externe Daten\Datenbasis Grundlage RDW.xlsm";
var sourceFile = DevelopmentWorkbook;
//var sourceFile = args.FirstOrDefault(); TODO: In Produktionsmodus aktivieren

if (string.IsNullOrWhiteSpace(sourceFile))
{
    Console.Error.WriteLine("Aufruf: Enset.Worker <Pfad-zur-Excel-Datei>");
    return;
}

if (!File.Exists(sourceFile))
{
    Console.Error.WriteLine($"Datei nicht gefunden: {sourceFile}");
    return;
}

var reader = new ExcelImportReader(
    new ExcelWorkbookReader(),
    sourceFile);

var coordinator = new ImportCoordinator(
    reader,
    new CustomerImportMapper(),
    new ExcelImportValidator(),
    new DuplicationCheckService(),
    new ConsoleImportLogger());

var runner = new ImportRunner(coordinator);

var report = await runner.RunAsync();

Console.WriteLine();
Console.WriteLine($"ImportId: {report.ImportId}");
Console.WriteLine($"CreatedAt: {report.CreatedAt:O}");
Console.WriteLine($"Decision: {report.Decision.Type} ({report.Decision.Reason})");
Console.WriteLine($"Customers: {report.CustomerCount}");
Console.WriteLine(
    $"Issues: {report.IssueCount} " +
    $"({report.ErrorCount} errors, {report.WarningCount} warnings)");

foreach (var issue in report.Issues)
{
    Console.WriteLine(
        $"{issue.IssueId} | {issue.Type} | {issue.Severity} | {issue.Message}");
}
