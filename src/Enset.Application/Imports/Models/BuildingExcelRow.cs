namespace Enset.Application.Imports.Models;

public class BuildingExcelRow
{
    public int RowNumber { get; set; }

    public string? InternalBuildingId { get; set; }
    public string? InternalCustomerId { get; set; }
    public string? FolderNumber { get; set; }
    public string? ProjectName { get; set; }
    public string? OrganizationName { get; set; }
    public string? BuildingType { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? HouseNumber { get; set; }
    public string? AddressAddition { get; set; }
    public string? ConstructionYear { get; set; }
    public string? RenovationYear { get; set; }
    public string? UsageType { get; set; }
    public string? MainUsage { get; set; }
    public string? AdditionalNote { get; set; }
    public string? FloorArea { get; set; }
    public string? HeatedFloorArea { get; set; }
    public string? HeatedFloorAreaMainUsage { get; set; }
    public string? HeatedFloorAreaSecondUsage { get; set; }
    public string? HeatConsumptionKWhPerYear { get; set; }
    public string? HeatingCostsEuroPerYear { get; set; }
    public string? ElectricityConsumptionKWhPerYear { get; set; }
    public string? ElectricityCostsEuroPerYear { get; set; }
    public string? HeatingType { get; set; }
    public string? HasPV { get; set; }
    public string? HasBattery { get; set; }
    public string? HwbCalculated { get; set; }
    public string? ElectricityConsumption { get; set; }
    public string? HeatConsumption { get; set; }
    public string? HotWaterConsumption { get; set; }
    public string? EnergyReferenceYear { get; set; }
    public string? MainUsageSharePercent { get; set; }
    public string? SecondaryUsage { get; set; }
    public string? SecondaryUsageSharePercent { get; set; }
    public string? Notes { get; set; }
    public string? BuildingName { get; set; }
}
