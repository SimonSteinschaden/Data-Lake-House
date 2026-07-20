namespace Enset.Application.Imports.DTOs;

public sealed class MeterReadingImportDto
{
    public string MeterNumber { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public decimal? Value { get; set; }

    public string? Unit { get; set; }

    public int? QualityFlag { get; set; }

    public string? ExternalCustomerId { get; set; }

    public string? ExternalBuildingId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? BuildingId { get; set; }

    public bool HasError { get; set; }

    public string? ErrorMessage { get; set; }
}