using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Mapping;

public static class MeterExcelRowMapper
{
    public static MeterImportDto ToDto(MeterExcelRow row) => new()
    {
        MeterNumber = row.MeterNumber?.Trim() ?? string.Empty,
        Name = row.Name,
        FileName = row.FileName?.Trim() ?? string.Empty,
        ProfileName = row.ProfileName,
        Unit = row.Unit,
        AnnualValue = row.AnnualValue,
        AnnualValueReferenceYear = row.AnnualValueReferenceYear,
        ExternalCustomerId = row.ExternalCustomerId,
        ExternalBuildingId = row.ExternalBuildingId
    };
}
