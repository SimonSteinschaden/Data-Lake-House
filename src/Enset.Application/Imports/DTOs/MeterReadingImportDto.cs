namespace Enset.Application.Imports.DTOs;

using Enset.Application.Imports.Enums;

public sealed class MeterReadingImportDto
{
    public string MeterNumber { get; set; } = string.Empty;
    public Guid? MeterId { get; set; }

    public DateTime? Timestamp { get; set; }
    public ImportFieldSource TimestampSource { get; set; }

    public decimal? Value { get; set; }
    public ImportFieldSource ValueSource { get; set; }

    public string? Unit { get; set; }

    public int? QualityFlag { get; set; }
    public ImportFieldSource QualitySource { get; set; }

    public int? RowNumber { get; set; }

    public string? MeterNumberRaw { get; set; }

    public string? TimestampRaw { get; set; }

    public string? ValueRaw { get; set; }

    public string? QualityRaw { get; set; }

    public string? ParsingError { get; set; }

    public string? ExternalCustomerId { get; set; }

    public string? ExternalBuildingId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? BuildingId { get; set; }

    public bool HasError { get; set; }

    public string? ErrorMessage { get; set; }
}
