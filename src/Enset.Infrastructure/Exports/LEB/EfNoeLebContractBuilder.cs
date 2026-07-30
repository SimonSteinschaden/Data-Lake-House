using Enset.Application.CanonicalSnapshots;
using Enset.Application.Exports.LEB.Abstractions;
using Enset.Application.Exports.LEB.Contracts;
using Enset.Application.Exports.LEB.Mapping;
using Enset.Application.Exports.LEB.Models;

namespace Enset.Infrastructure.Exports.LEB;

/// <summary>
/// The single LEB export projection. All fachliche values are mapped from the
/// same canonical dataset used by CRUD and Internal Data Products.
/// </summary>
public sealed class EfNoeLebContractBuilder(
    ICanonicalSnapshotReader snapshots,
    TimeProvider clock) : INoeLebContractBuilder
{
    public async Task<LebExportDataset> BuildAsync(
        LebExportRequest request,
        CancellationToken ct)
    {
        if (request.ReadingFrom.HasValue &&
            request.ReadingTo.HasValue &&
            request.ReadingFrom >= request.ReadingTo)
            throw new ArgumentException(
                "ReadingFrom must be before ReadingTo.");

        var dataset = await snapshots.GetPortfolio(ct);
        var buildings = dataset.Buildings
            .Where(x =>
                !request.CustomerId.HasValue ||
                x.CustomerId == request.CustomerId)
            .ToArray();
        var buildingIds = buildings
            .Select(x => x.BuildingId)
            .ToHashSet();
        var customers = dataset.Customers
            .ToDictionary(x => x.CustomerId);
        var meters = dataset.Meters
            .Where(x =>
                x.BuildingId.HasValue &&
                buildingIds.Contains(x.BuildingId.Value))
            .ToArray();
        var exportedAt = clock.GetUtcNow().UtcDateTime;

        var municipalityRows = buildings
            .Select(x => new
            {
                Building = x,
                MunicipalityId = ParseGuid(x.MunicipalityId)
            })
            .Where(x => x.MunicipalityId.HasValue)
            .GroupBy(x => x.MunicipalityId!.Value)
            .Select(group =>
            {
                var building = group.First().Building;
                return new LebMunicipalityRow(
                    group.Key,
                    building.MunicipalityNumber,
                    building.MunicipalityName ?? string.Empty,
                    building.MainRegion,
                    exportedAt);
            })
            .OrderBy(x => x.MunicipalityNumber)
            .ToArray();

        var objectRows = buildings.Select(building =>
        {
            customers.TryGetValue(
                building.CustomerId ?? Guid.Empty,
                out var customer);
            return new LebObjectRow(
                building.BuildingId,
                ParseGuid(building.MunicipalityId),
                building.BuildingType,
                building.BuildingNumber,
                building.Name,
                NoeBuildingUsageMapper.Map(building.UsageType),
                building.UsageType,
                Address(building.Street, building.HouseNumber),
                building.PostalCode,
                building.City,
                building.ConstructionYear,
                building.RenovationYear,
                building.NumberOfFloors,
                building.ConditionedFloorArea ?? building.HeatedArea,
                null,
                building.BuildingVolume,
                null,
                building.ConditionedFloorArea is null
                    ? null
                    : "ConditionedFloorArea",
                building.ConditionedFloorArea,
                building.ConditionedFloorArea is null ? null : "m²",
                customer?.ContactPerson,
                customer?.Phone,
                customer?.Email);
        }).ToArray();

        var meterRows = meters.Select(meter =>
        {
            var carrier = NoeEnergyCarrierMapper.Map(
                meter.Medium?.ToString());
            return new LebMeterRow(
                meter.MeterId,
                meter.BuildingId,
                meter.Name,
                meter.MeterNumber,
                meter.ExternalIdentifier,
                meter.MeterType,
                NoeMeterCategoryMapper.Map(meter.MeterType),
                carrier.Carrier,
                carrier.Medium,
                carrier.Group,
                NoeMeasurementDirectionMapper.Map(
                    meter.Direction?.ToString()),
                NoeReadingTypeMapper.Map(
                    meter.Readings.ReadingType?.ToString()),
                meter.Unit?.ToString(),
                meter.ValidFrom,
                meter.ValidTo);
        }).ToArray();

        var readingRows = meters
            .SelectMany(meter => meter.ReadingValues
                .Where(reading =>
                    (!request.ReadingFrom.HasValue ||
                     reading.Timestamp >= request.ReadingFrom) &&
                    (!request.ReadingTo.HasValue ||
                     reading.Timestamp < request.ReadingTo))
                .Select(reading => new LebReadingRow(
                    meter.MeterId,
                    reading.Timestamp,
                    reading.Value,
                    reading.Unit?.ToString(),
                    NoeReadingTypeMapper.Map(
                        reading.ReadingType.ToString()),
                    reading.QualityStatus,
                    reading.Source,
                    reading.IsCalculated)))
            .ToArray();

        var systemRows = dataset.EnergySystems
            .Where(x =>
                x.BuildingId.HasValue &&
                buildingIds.Contains(x.BuildingId.Value))
            .Select(system => new LebEnergySystemRow(
                system.EnergySystemId,
                system.BuildingId,
                system.Purpose ?? system.Type,
                system.EnergyCarrier ??
                    CarrierForSystem(system.Type),
                system.InstalledPower,
                system.ConstructionYear,
                system.ValidFrom,
                system.ValidTo))
            .ToArray();

        var contract = new NoeLebExportContractV1(
            NoeLebExportContractV1.Name,
            NoeLebExportContractV1.Version,
            exportedAt,
            municipalityRows,
            objectRows,
            meterRows,
            readingRows,
            systemRows);
        var assessments = buildings.Select(x => new LebExportAssessment(
                "Building",
                x.BuildingId,
                x.BuildingNumber,
                x.Quality.Level.ToString(),
                x.Suitability.Leb))
            .Concat(meters.Select(x => new LebExportAssessment(
                "Meter",
                x.MeterId,
                x.MeterNumber,
                x.Quality.Level.ToString(),
                x.Suitability.Leb)))
            .ToArray();
        var snapshotCreatedAt = buildings
            .Select(x => x.Version.CreatedAt)
            .Concat(meters.Select(x => x.Version.CreatedAt))
            .DefaultIfEmpty(exportedAt)
            .Max();
        return new(contract, assessments, snapshotCreatedAt);
    }

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;

    private static string? Address(
        string? street,
        string? houseNumber)
    {
        var value = string.Join(
            " ",
            new[] { street, houseNumber }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? CarrierForSystem(string? type)
    {
        var medium = type switch
        {
            "Photovoltaic" => "Electricity",
            "DistrictHeating" => "DistrictHeating",
            "HeatPump" or "Boiler" => "Heat",
            _ => null
        };
        return NoeEnergyCarrierMapper.Map(medium).Carrier;
    }
}
