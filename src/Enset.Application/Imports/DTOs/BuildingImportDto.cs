namespace Enset.Application.Imports.DTOs;

public sealed class BuildingImportDto
{
    public int SourceRowNumber { get; set; }

    public string? ExternalBuildingId { get; set; }

    public string? ExternalCustomerId { get; set; }

    public string? BuildingName { get; set; }

    public string? Street { get; set; }

    public string? HouseNumber { get; set; }

    public string? AddressAddition { get; set; }

    public string? PostalCode { get; set; }

    public string? City { get; set; }

    public string? Country { get; set; }

    public string? BuildingType { get; set; }

    public string? UsageType { get; set; }

    public int? ConstructionYear { get; set; }

    public int? RenovationYear { get; set; }

    public decimal? GrossFloorAreaM2 { get; set; }

    public decimal? NetFloorAreaM2 { get; set; }

    public decimal? ConditionedFloorAreaM2 { get; set; }

    public decimal? HeatedFloorAreaM2 { get; set; }

    public decimal? CooledFloorAreaM2 { get; set; }

    public decimal? BuildingVolumeM3 { get; set; }

    public int? NumberOfFloors { get; set; }

    public string? BuildingState { get; set; }
}
