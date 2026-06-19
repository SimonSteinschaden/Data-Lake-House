using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.DuplicationCheck.Mapping;
using Enset.Application.Imports.DuplicationCheck.Validation;
using Enset.Application.Imports.DuplicationCheck.Models;
using Enset.Application.Imports.DuplicationCheck.Resolutions;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Resolution;
using Enset.Application.Imports.Services;
using Enset.Application.Imports.Validation;
using Enset.Infrastructure.Exports.Excel;
using Enset.Infrastructure.Imports;
using Enset.Infrastructure.Imports.Excel;
using Enset.Infrastructure.Imports.Readers;
using XLWorkbook = ClosedXML.Excel.XLWorkbook;

var csvFilePath =
    @"C:\Users\rdpadmin\Desktop\Repositories\Data Lake House\Data-Lake-House-main\Data-Lake-House\Externe Daten\Lastprofil EEG.csv";

var excelFilePath =
    @"C:\Users\rdpadmin\Desktop\Repositories\Data Lake House\Data-Lake-House-main\Data-Lake-House\Externe Daten\Datenbasis Grundlage RDW.xlsm";

var exportFilePath =
    @"C:\Users\rdpadmin\Desktop\Repositories\Data Lake House\Data-Lake-House-main\Data-Lake-House\Exportierte Daten\Datenbasis Grundlage RDW_TEST.xlsm";


// 1. CSV-Test
RunCsvReaderTest(csvFilePath);


// 2. Excel-Struktur-Test
RunExcelStructureTest(excelFilePath);


// 3. Excel-Import
var excelReader = new ExcelWorkbookReader();

var customerRows = excelReader.ReadCustomers(excelFilePath);
var buildingRows = excelReader.ReadBuildings(excelFilePath);

Console.WriteLine();
Console.WriteLine("========== EXCEL IMPORT ==========");
Console.WriteLine($"Customers gelesen: {customerRows.Count}");
Console.WriteLine($"Buildings gelesen: {buildingRows.Count}");

PrintCustomerPreview(customerRows);
PrintBuildingPreview(buildingRows);


// 4. Import-Validation
var report = ExcelImportValidator.Validate(customerRows, buildingRows);

Console.WriteLine();
Console.WriteLine("========== IMPORT REPORT ==========");
Console.WriteLine($"Customers : {report.CustomerCount}");
Console.WriteLine($"Buildings : {report.BuildingCount}");
Console.WriteLine($"Errors    : {report.Errors.Count}");
Console.WriteLine($"Warnings  : {report.Warnings.Count}");

foreach (var error in report.Errors)
{
    Console.WriteLine($"ERROR: {error}");
}

foreach (var warning in report.Warnings)
{
    Console.WriteLine($"WARNING: {warning}");
}


// 5. Import-Entscheidung
var decision = ImportDecisionEngine.Decide(report);

Console.WriteLine();
Console.WriteLine($"Decision: {decision}");

if (decision == ImportDecisionType.Abort)
{
    Console.WriteLine("Validation errors found.");
    Console.WriteLine("Continuing anyway because this is currently a test run.");
}


// 6. Excel-Writer-Test
RunExcelWriterTest(
    excelFilePath,
    exportFilePath,
    customerRows,
    buildingRows);


// 7. Generic-ID-Test
RunGenericIdTest(
    excelFilePath,
    exportFilePath,
    customerRows,
    buildingRows);


// 8. DuplicationCheck mit User-Resolution
var customerDtos = customerRows
    .Select(CustomerExcelRowMapper.ToDto)
    .ToList();

RunCustomerDuplicationResolution(customerDtos);


// Ende
Console.WriteLine();
Console.WriteLine("========== END OF TESTING ==========");


// -------------------------
// Local test methods
// -------------------------

static void RunCsvReaderTest(string filePath)
{
    Console.WriteLine();
    Console.WriteLine("========== CSV READER TEST ==========");

    if (!File.Exists(filePath))
    {
        Console.WriteLine($"Datei nicht gefunden: {filePath}");
        return;
    }

    using var stream = File.OpenRead(filePath);

    var reader = new CsvMeterReadingReader();

    var readings = reader.Read(stream)
        .Take(100)
        .ToList();

    Console.WriteLine($"Gelesene Datensätze: {readings.Count}");
    Console.WriteLine($"Erster Datensatz: {readings.FirstOrDefault()?.Timestamp}");
    Console.WriteLine($"Letzter Datensatz: {readings.LastOrDefault()?.Timestamp}");
}


static void RunExcelStructureTest(string filePath)
{
    Console.WriteLine();
    Console.WriteLine("========== EXCEL STRUCTURE TEST ==========");

    using var workbook = new XLWorkbook(filePath);

    PrintWorksheetStructure(workbook, "Customers");
    PrintWorksheetStructure(workbook, "Buildings");
}


static void PrintWorksheetStructure(XLWorkbook workbook, string worksheetName)
{
    var worksheet = workbook.Worksheet(worksheetName);

    Console.WriteLine();
    Console.WriteLine($"Worksheet: {worksheet.Name}");

    foreach (var table in worksheet.Tables)
    {
        Console.WriteLine($"Table: {table.Name}");
        Console.WriteLine("Columns:");

        foreach (var field in table.Fields)
        {
            Console.WriteLine($" - {field.Name}");
        }

        Console.WriteLine($"Rows: {table.DataRange.RowCount()}");
    }
}


static void PrintCustomerPreview(IEnumerable<dynamic> customers)
{
    Console.WriteLine();
    Console.WriteLine("========== CUSTOMER PREVIEW ==========");

    foreach (var customer in customers.Take(10))
    {
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"Row: {customer.RowNumber}");
        Console.WriteLine($"ID: {customer.InternalCustomerId}");
        Console.WriteLine($"Name: {customer.FirstName} {customer.LastName}");
        Console.WriteLine($"Organization: {customer.OrganizationName}");
    }
}


static void PrintBuildingPreview(IEnumerable<dynamic> buildings)
{
    Console.WriteLine();
    Console.WriteLine("========== BUILDING PREVIEW ==========");

    foreach (var building in buildings.Take(5))
    {
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"Row: {building.RowNumber}");
        Console.WriteLine($"BuildingID: {building.InternalBuildingId}");
        Console.WriteLine($"CustomerID: {building.InternalCustomerId}");
        Console.WriteLine($"Project: {building.ProjectName}");
        Console.WriteLine($"Address: {building.Street} {building.HouseNumber}, {building.PostalCode} {building.City}");
    }
}


static void RunExcelWriterTest(
    string sourceFile,
    string targetFile,
    IEnumerable<dynamic> customers,
    IEnumerable<dynamic> buildings)
{
    Console.WriteLine();
    Console.WriteLine("========== EXCEL WRITER TEST ==========");

    var writer = new ExcelWorkbookWriter();

    var firstCustomerId = customers
        .First(c => !string.IsNullOrWhiteSpace(c.InternalCustomerId))
        .InternalCustomerId;

    var customerSuccess = writer.UpdateCustomerNotes(
        sourceFile,
        targetFile,
        firstCustomerId,
        $"BUSINESS UPDATE {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

    Console.WriteLine(customerSuccess
        ? "Customer notes updated."
        : "Customer not found.");

    var firstBuildingId = buildings
        .First(b => !string.IsNullOrWhiteSpace(b.InternalBuildingId))
        .InternalBuildingId;

    var buildingSuccess = writer.UpdateBuildingNotes(
        sourceFile,
        targetFile,
        firstBuildingId,
        $"BUILDING UPDATE {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

    Console.WriteLine(buildingSuccess
        ? "Building notes updated."
        : "Building not found.");
}


static void RunGenericIdTest(
    string sourceFile,
    string targetFile,
    IEnumerable<dynamic> customers,
    IEnumerable<dynamic> buildings)
{
    Console.WriteLine();
    Console.WriteLine("========== GENERIC ID TEST ==========");

    var writer = new ExcelWorkbookWriter();

    var customerWithoutId = customers
        .FirstOrDefault(c => string.IsNullOrWhiteSpace(c.InternalCustomerId));

    if (customerWithoutId is null)
    {
        Console.WriteLine("No customer without InternalCustomerId found.");
    }
    else
    {
        var newId = CustomerIdGenerator.Generate();

        var idUpdateSuccess = writer.UpdateCustomerId(
            sourceFile,
            targetFile,
            customerWithoutId.RowNumber,
            newId);

        Console.WriteLine(idUpdateSuccess
            ? $"Customer row {customerWithoutId.RowNumber} updated with ID {newId}."
            : $"Customer row {customerWithoutId.RowNumber} could not be updated.");
    }

    var buildingWithoutId = buildings
        .FirstOrDefault(b => string.IsNullOrWhiteSpace(b.InternalBuildingId));

    if (buildingWithoutId is null)
    {
        Console.WriteLine("No building without InternalBuildingId found.");
    }
    else
    {
        var newBuildingId = BuildingIdGenerator.Generate();

        var buildingIdUpdateSuccess = writer.UpdateBuildingId(
            sourceFile,
            targetFile,
            buildingWithoutId.RowNumber,
            newBuildingId);

        Console.WriteLine(buildingIdUpdateSuccess
            ? $"Building row {buildingWithoutId.RowNumber} updated with ID {newBuildingId}."
            : $"Building row {buildingWithoutId.RowNumber} could not be updated.");
    }
}


static void RunCustomerDuplicationResolution(
    List<CustomerImportDto> customers)
{
    Console.WriteLine();
    Console.WriteLine("========== DUPLICATION CHECK ==========");

    var validator = new CustomerDuplicateValidator();

    var duplicateCandidates = validator.FindDuplicates(customers);

    Console.WriteLine($"Dubletten gefunden: {duplicateCandidates.Count}");

    if (duplicateCandidates.Count == 0)
    {
        return;
    }

    var issues = duplicateCandidates
        .Select(CustomerDuplicateIssueMapper.ToIssue)
        .ToList();

    var resolutionService = new ConsoleImportIssueResolutionService();

    var resolutionCompleted = resolutionService.ResolveIssues(issues);

    if (!resolutionCompleted)
    {
        Console.WriteLine("Import wurde durch Benutzer abgebrochen.");
        return;
    }

    var resultBuilder = new ImportResolutionResultBuilder();
    var mergeInstructionBuilder = new CustomerMergeInstructionBuilder();

    var customerMergeInstructions = new List<CustomerMergeInstruction>();

    for (var i = 0; i < duplicateCandidates.Count; i++)
    {
        var issue = issues[i];

        if (!issue.IsResolved)
        {
            continue;
        }

        var result = resultBuilder.Build(issue);

        var instruction = mergeInstructionBuilder.Build(
            duplicateCandidates[i],
            result);

        if (instruction is not null)
        {
            customerMergeInstructions.Add(instruction);
        }
    }

var groupBuilder = new CustomerMergeGroupBuilder();

var customerMergeGroups = groupBuilder.Build(customerMergeInstructions);

Console.WriteLine();
Console.WriteLine($"Customer Merge Groups: {customerMergeGroups.Count}");

foreach (var group in customerMergeGroups)
{
    Console.WriteLine("--------------------------------");
    Console.WriteLine($"Master: {group.MasterCustomer.CompanyName}");
    Console.WriteLine($"Name:   {group.ResolvedName}");

    Console.WriteLine("Duplicates:");

    foreach (var duplicate in group.DuplicateCustomers)
    {
        Console.WriteLine($" - {duplicate.CompanyName}");
    }
}
}