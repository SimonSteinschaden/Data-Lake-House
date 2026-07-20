using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Mapping;

public static class BuildingExcelRowMapper
{
    public static BuildingImportDto ToDto(BuildingExcelRow row) => new()
    {
        ExternalBuildingId = row.InternalBuildingId,
        ExternalCustomerId = row.InternalCustomerId,
        BuildingName = row.BuildingName ?? row.ProjectName,
        Street = row.Street,
        HouseNumber = row.HouseNumber,
        PostalCode = row.PostalCode,
        City = row.City,
        Country = row.Country,
        BuildingType = row.BuildingType
    };
}
