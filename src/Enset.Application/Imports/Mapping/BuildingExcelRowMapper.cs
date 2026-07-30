using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Mapping;

public static class BuildingExcelRowMapper
{
    public static BuildingImportDto ToDto(BuildingExcelRow row) => new()
    {
        SourceRowNumber = row.RowNumber,
        ExternalBuildingId = row.InternalBuildingId,
        ExternalCustomerId = row.InternalCustomerId,
        BuildingName = row.BuildingName ?? row.ProjectName,
        Street = row.Street,
        HouseNumber = row.HouseNumber,
        AddressAddition = row.AddressAddition,
        PostalCode = row.PostalCode,
        City = row.City,
        Country = row.Country,
        BuildingType = Normalize(row.BuildingType),
        UsageType = Normalize(row.UsageType ?? row.MainUsage),
        ConstructionYear = ParseInt(row.ConstructionYear),
        RenovationYear = ParseInt(row.RenovationYear),
        GrossFloorAreaM2 = ParseDecimal(row.FloorArea),
        NetFloorAreaM2 = ParseDecimal(row.NetFloorArea),
        ConditionedFloorAreaM2 = ParseDecimal(row.ConditionedFloorArea),
        HeatedFloorAreaM2 = ParseDecimal(row.HeatedFloorArea),
        CooledFloorAreaM2 = ParseDecimal(row.CooledFloorArea),
        BuildingVolumeM3 = ParseDecimal(row.BuildingVolume),
        NumberOfFloors = ParseInt(row.NumberOfFloors),
        BuildingState = Normalize(row.BuildingState)
    };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? ParseInt(string? value) =>
        int.TryParse(value?.Trim(), out var parsed) ? parsed : null;

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim().Replace(" ", string.Empty);
        var cultures = new[]
        {
            System.Globalization.CultureInfo.GetCultureInfo("de-AT"),
            System.Globalization.CultureInfo.InvariantCulture
        };
        return cultures
            .Select(culture => decimal.TryParse(
                normalized,
                System.Globalization.NumberStyles.Number,
                culture,
                out var parsed)
                ? (decimal?)parsed
                : null)
            .FirstOrDefault(parsed => parsed.HasValue);
    }
}
