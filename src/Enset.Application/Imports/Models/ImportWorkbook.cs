namespace Enset.Application.Imports.Models;

using Enset.Application.Imports.Enums;

public sealed class ImportWorkbook
{
    public ImportSourceType SourceType { get; init; } = ImportSourceType.Excel;
    public IReadOnlyList<CustomerExcelRow> Customers { get; init; } = [];
    public IReadOnlyList<BuildingExcelRow> Buildings { get; init; } = [];
    public IReadOnlyList<MeterExcelRow> Meters { get; init; } = [];
    public IReadOnlyList<MeterReadingExcelRow> MeterReadings { get; init; } = [];
    public CsvMeterReadingMapping? CsvMapping { get; init; }
}
