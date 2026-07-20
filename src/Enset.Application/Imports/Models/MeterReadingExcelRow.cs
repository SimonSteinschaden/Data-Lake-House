namespace Enset.Application.Imports.Models;

public sealed class MeterReadingExcelRow
{
    public int RowNumber { get; set; }
    public string? MeterNumber { get; set; }
    public string? Timestamp { get; set; }
    public string? Value { get; set; }
    public string? Unit { get; set; }
    public string? QualityFlag { get; set; }
}
