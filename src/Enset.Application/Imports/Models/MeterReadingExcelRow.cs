namespace Enset.Application.Imports.Models;

using Enset.Application.Imports.Enums;

public sealed class MeterReadingExcelRow
{
    public int RowNumber { get; set; }
    public string? MeterNumber { get; set; }
    public Guid? MeterId { get; set; }
    public string? DefaultMeterNumber { get; set; }
    public string? Timestamp { get; set; }
    public ImportFieldSource TimestampSource { get; set; } =
        ImportFieldSource.FileColumn;
    public string? Value { get; set; }
    public ImportFieldSource ValueSource { get; set; } =
        ImportFieldSource.FileColumn;
    public string? Unit { get; set; }
    public string? QualityFlag { get; set; }
    public ImportFieldSource QualitySource { get; set; } =
        ImportFieldSource.FileColumn;
    public string? ParsingError { get; set; }
}
