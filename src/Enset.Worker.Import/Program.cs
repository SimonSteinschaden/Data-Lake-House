using Enset.Infrastructure.Imports.Readers;
using Enset.Infrastructure.Imports.Excel;
using Enset.Infrastructure.Exports.Excel;
using Enset.Infrastructure.Imports;
using Enset.Application.Imports.Abstractions;
using Enset.Application.Imports.Reports;
using Enset.Application.Imports.Mapping;
using Enset.Application.Imports.Validation;
using Enset.Application.Imports.DuplicationCheck.Validation;
using Enset.Application.Imports.Decisions;
using Enset.Application.Imports.Services;
using Enset.Application.Imports.Services.Abstractions;
using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Issues;
using XLWorkbook = ClosedXML.Excel.XLWorkbook;

//------------------------------TESTING CSV READER------------------------------
var filePath =
    @"C:\Users\rdpadmin\Desktop\Repositories\Data Lake House\Data-Lake-House-main\Data-Lake-House\Externe Daten\Lastprofil EEG.csv"; //TODO: Pfad anpassen. Pfadlogik welches Dokument geladen werden soll fehlt noch Automatische Erkennung.

if (!File.Exists(filePath))
{
    Console.WriteLine($"Datei nicht gefunden: {filePath}");
    return;
}

using var stream = File.OpenRead(filePath);

var reader = new CsvMeterReadingReader();

/*foreach (var reading in reader.Read(stream).Take(100))
{
    Console.WriteLine("--------------------------------");
    Console.WriteLine($"MeterNumber : {reading.MeterNumber}");
    Console.WriteLine($"Timestamp   : {reading.Timestamp}");
    Console.WriteLine($"Value       : {reading.Value}");
    Console.WriteLine($"Unit        : {reading.Unit}");
    Console.WriteLine($"QualityFlag : {reading.QualityFlag}");
}*/

var readings = reader.Read(stream)
    .Take(100)
    .ToList();

Console.WriteLine($"Gelesene Datensätze: {readings.Count}");
Console.WriteLine($"Erster Datensatz: {readings.FirstOrDefault()?.Timestamp}");
Console.WriteLine($"Letzter Datensatz: {readings.LastOrDefault()?.Timestamp}");



//------------------------------TESTING EXCEL READER------------------------------

var filePath2 = 
    @"C:\Users\rdpadmin\Desktop\Repositories\Data Lake House\Data-Lake-House-main\Data-Lake-House\Externe Daten\Datenbasis Grundlage RDW.xlsm"; //TODO: Pfad anpassen
using var workbook = new XLWorkbook(filePath2);

var worksheet = workbook.Worksheet("Customers");

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

var worksheet2 = workbook.Worksheet("Buildings");

Console.WriteLine($"Worksheet: {worksheet2.Name}");

foreach (var table in worksheet2.Tables)
{
    Console.WriteLine($"Table: {table.Name}");

    Console.WriteLine("Columns:");

    foreach (var field in table.Fields)
    {
        Console.WriteLine($" - {field.Name}");
    }

    Console.WriteLine($"Rows: {table.DataRange.RowCount()}");
}

//------------------------------TESTING EXCEL READER CLASS CUSTOMERS------------------------------

var excelReader = new ExcelWorkbookReader();

var customers = excelReader.ReadCustomers(filePath2);
var buildings = excelReader.ReadBuildings(filePath2);



Console.WriteLine($"Customers gelesen: {customers.Count}");
Console.WriteLine($"Buildings gelesen: {buildings.Count}");
foreach (var customer in customers.Take(10))
{
    Console.WriteLine("--------------------------------");
    Console.WriteLine($"Row: {customer.RowNumber}");
    Console.WriteLine($"ID: {customer.InternalCustomerId}");
    Console.WriteLine($"Name: {customer.FirstName} {customer.LastName}");
    Console.WriteLine($"Organization: {customer.OrganizationName}");
}

//------------------------------TESTING EXCEL READER CLASS BUILDINGS------------------------------


foreach (var building in buildings.Take(5))
{
    Console.WriteLine("--------------------------------");
    Console.WriteLine($"Row: {building.RowNumber}");
    Console.WriteLine($"BuildingID: {building.InternalBuildingId}");
    Console.WriteLine($"CustomerID: {building.InternalCustomerId}");
    Console.WriteLine($"Project: {building.ProjectName}");
    Console.WriteLine($"Address: {building.Street} {building.HouseNumber}, {building.PostalCode} {building.City}");
}

//------------------------------REPORTING EXCEL VALIDATION------------------------------

var report =
    ExcelImportValidator.Validate(
        customers,
        buildings);

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

//-------------------------------DECISION LOGIC BASED ON VALIDATION------------------------------
var decision = ImportDecisionEngine.Decide(report);

Console.WriteLine();
Console.WriteLine($"Decision: {decision}");

if (decision == ImportDecisionType.Abort)
{
    Console.WriteLine("Validation errors found.");
    Console.WriteLine("Continuing anyway because this is a writer test."); //return;
}


//-------------------------------EXPORT EXCEL WORKBOOK-COPY------------------------------
var writer = new ExcelWorkbookWriter();

var targetFile =
    @"C:\Users\rdpadmin\Desktop\Repositories\Data Lake House\Data-Lake-House-main\Data-Lake-House\Exportierte Daten\Datenbasis Grundlage RDW_TEST.xlsm";

var firstCustomerId = customers
    .First(c => !string.IsNullOrWhiteSpace(c.InternalCustomerId))
    .InternalCustomerId!;

var success = writer.UpdateCustomerNotes(
    filePath2,
    targetFile,
    firstCustomerId,
    $"BUSINESS UPDATE {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

Console.WriteLine(success
    ? "Customer notes updated."
    : "Customer not found.");



        var firstBuildingId = buildings
            .First(b => !string.IsNullOrWhiteSpace(b.InternalBuildingId))
            .InternalBuildingId!;

        var buildingSuccess = writer.UpdateBuildingNotes(
            filePath2,
            targetFile,
            firstBuildingId,
            $"BUILDING UPDATE {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        Console.WriteLine(buildingSuccess
            ? "Building notes updated."
            : "Building not found.");


//-------------------------------TESTING GENERIC ID ------------------------------

var customerWithoutId =
    customers.FirstOrDefault(
        c => string.IsNullOrWhiteSpace(c.InternalCustomerId));

if (customerWithoutId is null)
{
    Console.WriteLine("No customer without InternalCustomerId found.");
}
else
{
    var newId = CustomerIdGenerator.Generate();

    var idUpdateSuccess = writer.UpdateCustomerId(
    filePath2,
    targetFile,
    customerWithoutId.RowNumber,
    newId);

    Console.WriteLine(success
        ? $"Customer row {customerWithoutId.RowNumber} updated with ID {newId}."
        : $"Customer row {customerWithoutId.RowNumber} could not be updated.");
}

var buildingWithoutId =
    buildings.FirstOrDefault(
        b => string.IsNullOrWhiteSpace(b.InternalBuildingId));

if (buildingWithoutId is null)
{
    Console.WriteLine("No building without InternalBuildingId found.");
}
else
{
    var newBuildingId = BuildingIdGenerator.Generate();

    var buildingIdUpdateSuccess = writer.UpdateBuildingId(
        filePath2,
        targetFile,
        buildingWithoutId.RowNumber,
        newBuildingId);

    Console.WriteLine(buildingIdUpdateSuccess
        ? $"Building row {buildingWithoutId.RowNumber} updated with ID {newBuildingId}."
        : $"Building row {buildingWithoutId.RowNumber} could not be updated.");
}

//-------------------------------Duplication Check-------------------------------


var excelReader2 = new ExcelWorkbookReader();

var customerRows = excelReader2.ReadCustomers(filePath2);

var customers2 = customerRows
    .Select(CustomerExcelRowMapper.ToDto)
    .ToList();

Console.WriteLine($"Customer rows gelesen: {customerRows.Count}");
Console.WriteLine($"Customer DTOs erzeugt: {customers2.Count}");

var validator = new CustomerDuplicateValidator();

var duplicates = validator.FindDuplicates(customers2);

Console.WriteLine($"Dubletten gefunden: {duplicates.Count}");

foreach (var duplicate in duplicates)
{
    Console.WriteLine("--------------------------------");
    Console.WriteLine($"First : {duplicate.First.CompanyName}");
    Console.WriteLine($"Second: {duplicate.Second.CompanyName}");
    Console.WriteLine($"Score : {duplicate.SimilarityScore:P1}");
    Console.WriteLine($"Reason: {duplicate.Reason}");
}

//-------------------------------END OF TESTING-------------------------------