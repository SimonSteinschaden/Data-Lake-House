using Enset.Application.Imports.DTOs;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Mapping;

public static class MeterExcelRowMapper
{
    public static MeterImportDto ToDto(MeterExcelRow row) => new()
    {
        MeterNumber = row.MeterNumber?.Trim() ?? string.Empty,
        FileName = row.FileName?.Trim() ?? string.Empty,
        ProfileName = row.ProfileName,
        Unit = row.Unit,
        ExternalCustomerId = row.ExternalCustomerId,
        ExternalBuildingId = row.ExternalBuildingId
    };
}
