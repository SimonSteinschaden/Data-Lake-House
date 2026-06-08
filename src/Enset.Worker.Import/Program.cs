using Enset.Infrastructure.Imports.Readers;

var filePath =
    @"C:\Users\rdpadmin\Desktop\Repositories\Data Lake House\Data-Lake-House-main\Data-Lake-House\Externe Daten\Lastprofil EEG.csv";

if (!File.Exists(filePath))
{
    Console.WriteLine($"Datei nicht gefunden: {filePath}");
    return;
}

using var stream = File.OpenRead(filePath);

var reader = new CsvMeterReadingReader();

foreach (var reading in reader.Read(stream))
{
    Console.WriteLine("--------------------------------");
    Console.WriteLine($"MeterNumber : {reading.MeterNumber}");
    Console.WriteLine($"Timestamp   : {reading.Timestamp}");
    Console.WriteLine($"Value       : {reading.Value}");
    Console.WriteLine($"Unit        : {reading.Unit}");
    Console.WriteLine($"QualityFlag : {reading.QualityFlag}");
}