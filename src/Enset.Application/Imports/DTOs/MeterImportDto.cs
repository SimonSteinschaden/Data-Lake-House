namespace Enset.Application.Imports.DTOs;

public sealed class MeterImportDto
{
    public string MeterNumber { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string? ProfileName { get; set; }

    public string? Unit { get; set; }

    public string? PostalCode { get; set; }

    public string? ExternalCustomerId { get; set; }

    public string? ExternalBuildingId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? BuildingId { get; set; }
}