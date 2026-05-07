namespace Enset.Application.Imports.DTOs;

public class MeterImportDto
{
    public string MeterNumber { get; set; } = null!;
    public string FileName { get; set; } = null!;

    public string? ProfileName { get; set; }
    public string? Unit { get; set; }

    public string? PostalCode { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? CustomerId { get; set; }
}