namespace Enset.Application.Imports.Models;

public sealed class MeterExcelRow
{
    public int RowNumber { get; set; }
    public string? MeterNumber { get; set; }
    public string? FileName { get; set; }
    public string? ProfileName { get; set; }
    public string? Unit { get; set; }
    public string? ExternalCustomerId { get; set; }
    public string? ExternalBuildingId { get; set; }
}
