using Enset.Application.Imports.Coordination;
using Enset.Application.Imports.DuplicationCheck.Services;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Validation;
using Enset.Application.Imports.WriteGate;
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

var outputFile = Path.Combine(
    Path.GetDirectoryName(sourceFile)!,
    $"{Path.GetFileNameWithoutExtension(sourceFile)}_imported{Path.GetExtension(sourceFile)}");

File.Copy(sourceFile, outputFile, overwrite: true);

var reader = new ExcelImportReader(
    new ExcelWorkbookReader(),
    sourceFile);

IExcelWorkbookWriter workbookWriter =
    new ExcelWorkbookWriter(outputFile);

var coordinator = new ImportCoordinator(
    reader,
    new CustomerImportMapper(),
    new ExcelImportValidator(),
    new DuplicationCheckService(),
    new ImportWriteGate(),
    new ExcelImportWriter(workbookWriter),
    new ConsoleImportLogger());

var runner = new ImportRunner(coordinator);

await runner.RunAsync();
