namespace Enset.Application.Imports.Models;

public sealed class ImportWorkbook
{
    public IReadOnlyList<CustomerExcelRow> Customers { get; init; } = [];
    public IReadOnlyList<BuildingExcelRow> Buildings { get; init; } = [];
    public IReadOnlyList<MeterExcelRow> Meters { get; init; } = [];
    public IReadOnlyList<MeterReadingExcelRow> MeterReadings { get; init; } = [];
}
