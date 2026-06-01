using Enset.Infrastructure.Imports.Excel;

var filePath =
    @"C:\Users\simon\Desktop\Repositories\Data-Lake-House\Externe Daten\Imports\meter-import.xlsx";

if (!File.Exists(filePath))
{
    Console.WriteLine($"Datei nicht gefunden: {filePath}");
    return;
}

using var stream = File.OpenRead(filePath);

var reader = new ExcelMeterImportReader();

var meters = reader.Read(stream);

foreach (var meter in meters)
{
    Console.WriteLine("--------------------------------");

    Console.WriteLine($"MeterNumber : {meter.MeterNumber}");
    Console.WriteLine($"ProfileName : {meter.ProfileName}");
    Console.WriteLine($"FileName    : {meter.FileName}");
    Console.WriteLine($"Unit        : {meter.Unit}");
    Console.WriteLine($"PostalCode  : {meter.PostalCode}");
}