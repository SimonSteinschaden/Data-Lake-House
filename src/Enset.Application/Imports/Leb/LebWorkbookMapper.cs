using System.Globalization;
using Enset.Application.Imports.Enums;
using Enset.Application.Imports.Leb.DTOs;
using Enset.Application.Imports.Models;

namespace Enset.Application.Imports.Leb;

public sealed class LebWorkbookMapper
{
    public ImportWorkbook Map(LebWorkbookDto source, ImportMedium medium)
    {
        ArgumentNullException.ThrowIfNull(source);

        var validIdentityRows = source.Rows
            .Where(x => !string.IsNullOrWhiteSpace(x.MunicipalityId) &&
                        !string.IsNullOrWhiteSpace(x.BuildingId))
            .ToList();

        var customers = validIdentityRows
            .GroupBy(x => x.MunicipalityId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new CustomerExcelRow
            {
                RowNumber = group.First().RowNumber,
                InternalCustomerId = LebExternalIdentity.Municipality(group.Key),
                OrganizationName = group.Select(x => x.MunicipalityName)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? group.Key,
                Country = "AT"
            })
            .ToList();

        var buildings = validIdentityRows
            .GroupBy(x => new
            {
                MunicipalityId = x.MunicipalityId!.Trim().ToUpperInvariant(),
                BuildingId = x.BuildingId!.Trim().ToUpperInvariant()
            })
            .Select(group =>
            {
                var first = group.First();
                return new BuildingExcelRow
                {
                    RowNumber = first.RowNumber,
                    InternalCustomerId = LebExternalIdentity.Municipality(first.MunicipalityId!),
                    InternalBuildingId = LebExternalIdentity.Building(
                        first.MunicipalityId!, first.BuildingId!),
                    BuildingName = first.BuildingName,
                    ProjectName = first.BuildingName,
                    OrganizationName = first.MunicipalityName,
                    City = first.MunicipalityName,
                    ConstructionYear = first.ConstructionYear,
                    ConditionedFloorArea = first.FloorArea,
                    Country = "AT"
                };
            })
            .ToList();

        var meterRows = validIdentityRows
            .Where(x => !string.IsNullOrWhiteSpace(x.MeterId))
            .ToList();
        var meters = meterRows
            .GroupBy(x => x.MeterId!.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return new MeterExcelRow
                {
                    RowNumber = first.RowNumber,
                    MeterNumber = group.Key,
                    Name = first.MeterName,
                    ProfileName = medium.ToString(),
                    Unit = first.Unit,
                    AnnualValue = ParseDecimal(
                        group.OrderByDescending(x => ParseYear(x.Year))
                            .Select(x => x.AnnualValue)
                            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))),
                    AnnualValueReferenceYear = group
                        .Where(x => !string.IsNullOrWhiteSpace(x.AnnualValue))
                        .Select(x => ParseYear(x.Year))
                        .Where(x => x.HasValue)
                        .OrderByDescending(x => x)
                        .FirstOrDefault(),
                    ExternalCustomerId = LebExternalIdentity.Municipality(first.MunicipalityId!),
                    ExternalBuildingId = LebExternalIdentity.Building(
                        first.MunicipalityId!, first.BuildingId!)
                };
            })
            .ToList();

        var readings = new List<MeterReadingExcelRow>();
        foreach (var row in meterRows)
        {
            var meterNumber = row.MeterId!.Trim();
            if (!int.TryParse(row.Year?.Trim(), out var year))
            {
                readings.Add(new MeterReadingExcelRow
                {
                    RowNumber = row.RowNumber,
                    MeterNumber = meterNumber,
                    ParsingError = $"Jahr '{row.Year}' ist ungültig"
                });
                continue;
            }

            for (var month = 1; month <= 12; month++)
            {
                var value = row.MonthlyValues.ElementAtOrDefault(month - 1);
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                readings.Add(new MeterReadingExcelRow
                {
                    RowNumber = row.RowNumber,
                    MeterNumber = meterNumber,
                    Timestamp = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc)
                        .ToString("O"),
                    Value = value,
                    Unit = row.Unit,
                    ReadingType = Enset.Domain.Energy.MeterReadingType.IntervalValue
                });
            }
        }

        return new ImportWorkbook
        {
            SourceType = ImportSourceType.Landesenergiebuchhaltung,
            Customers = customers,
            Buildings = buildings,
            Meters = meters,
            MeterReadings = readings
        };
    }

    private static int? ParseYear(string? value) =>
        int.TryParse(value?.Trim(), out var year) ? year : null;

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var cultures = new[]
        {
            CultureInfo.GetCultureInfo("de-AT"),
            CultureInfo.InvariantCulture
        };
        foreach (var culture in cultures)
        {
            if (decimal.TryParse(
                    value.Trim(),
                    NumberStyles.Number,
                    culture,
                    out var number))
                return number;
        }
        return null;
    }
}
