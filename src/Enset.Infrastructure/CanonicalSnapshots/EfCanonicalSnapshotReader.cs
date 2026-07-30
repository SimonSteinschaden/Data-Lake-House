using System.Globalization;
using Enset.Application.Authorization;
using Enset.Application.CanonicalSnapshots;
using Enset.Domain.Curation;
using Enset.Domain.Data;
using Enset.Domain.Energy;
using Enset.Domain.GoldProfiles;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.CanonicalSnapshots;

public sealed class EfCanonicalSnapshotReader(
    EnsetDbContext db,
    IDataAccessScope scope,
    TimeProvider clock) : ICanonicalSnapshotReader
{
    public async Task<CustomerCanonicalSnapshot?> GetCustomer(
        Guid id, CancellationToken ct)
    {
        var customer = await scope.ApplyCustomerScope(db.Customers)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return customer is null ? null : new(
            customer.Id,
            customer.CustomerNumber,
            customer.Name,
            customer.ContactPerson,
            customer.Email,
            customer.Phone,
            customer.PostalCode,
            customer.City,
            null,
            null,
            customer.IsActive,
            Quality([
                customer.CustomerNumber,
                customer.Name,
                customer.City
            ], EmptyFields),
            Suitability(customer.CustomerNumber, customer.Name),
            await Version("Customer", customer.Id, ct));
    }

    public async Task<BuildingCanonicalSnapshot?> GetBuilding(
        Guid id, CancellationToken ct)
    {
        var building = await scope.ApplyBuildingScope(db.Buildings)
            .AsNoTracking()
            .Include(x => x.Versions)
                .ThenInclude(x => x.Address)
                    .ThenInclude(x => x!.PostalCodeArea)
            .Include(x => x.Versions)
                .ThenInclude(x => x.Address)
                    .ThenInclude(x => x!.Municipality)
                        .ThenInclude(x => x!.Regions)
            .Include(x => x.CustomerAssignments)
                .ThenInclude(x => x.Customer)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (building is null)
            return null;

        var curated = await Fields("Building", id, ct);
        var version = building.Versions
            .Where(x => x.ValidTo == null)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefault();
        var customer = building.CustomerAssignments
            .Where(x => x.ValidTo == null)
            .OrderByDescending(x => x.IsPrimary)
            .Select(x => x.Customer)
            .FirstOrDefault();
        string? Field(string name, string? original) =>
            Curated(curated, name) ?? Clean(original);
        var buildingType = Field(
            "BuildingCategory",
            version?.BuildingCategory?.ToString());
        var usageType = Field(
            "PrimaryUseType",
            version?.PrimaryUseType?.ToString());
        var postalCode = Field(
            "PostalCode",
            version?.Address?.PostalCodeArea?.Code);
        var city = Field(
            "City",
            version?.Address?.City ??
            version?.Address?.Municipality?.Name);
        var conditioned = Decimal(
            Field(
                "ConditionedFloorAreaM2",
                version?.ConditionedFloorAreaM2?.ToString(
                    CultureInfo.InvariantCulture)));
        var quality = Quality(
            [
                building.BuildingNumber,
                building.Name,
                usageType,
                postalCode,
                conditioned?.ToString(CultureInfo.InvariantCulture)
            ],
            curated);

        return new(
            building.Id,
            building.BuildingNumber,
            building.Name,
            customer?.Id,
            customer?.CustomerNumber,
            customer?.Name,
            buildingType,
            usageType,
            Field("BuildingState", null),
            Field("Street", version?.Address?.Street),
            Field("HouseNumber", version?.Address?.HouseNumber),
            postalCode,
            city,
            version?.Address?.MunicipalityId?.ToString(),
            version?.Address?.Municipality?.Name,
            Integer(Field(
                "ConstructionYear",
                version?.YearOfConstruction?.ToString())),
            Integer(Field(
                "RenovationYear",
                version?.YearOfLastMajorRenovation?.ToString())),
            Decimal(Field("GrossFloorAreaM2",
                Invariant(version?.GrossFloorAreaM2))),
            Decimal(Field("NetFloorAreaM2",
                Invariant(version?.NetFloorAreaM2))),
            conditioned,
            Decimal(Field("HeatedAreaSquareMeters",
                Invariant(version?.HeatedFloorAreaM2))),
            Decimal(Field("CooledFloorAreaM2",
                Invariant(version?.CooledFloorAreaM2))),
            Decimal(Field("BuildingVolumeM3",
                Invariant(version?.BuildingVolumeM3))),
            Integer(Field(
                "NumberOfFloors",
                version?.NumberOfFloors?.ToString())),
            building.IsActive,
            quality,
            Suitability(usageType, conditioned),
            await Version("Building", id, ct))
        {
            MunicipalityNumber =
                version?.Address?.Municipality?.Code,
            MainRegion = version?.Address?.Municipality?.Regions
                .Select(x => x.Name)
                .FirstOrDefault()
        };
    }

    public async Task<MeterCanonicalSnapshot?> GetMeter(
        Guid id, CancellationToken ct)
    {
        var row = await scope.ApplyMeterScope(db.Meters)
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                Meter = x,
                Building = x.Building,
                Customer = x.Building == null
                    ? null
                    : x.Building.CustomerAssignments
                        .Where(a => a.ValidTo == null)
                        .OrderByDescending(a => a.IsPrimary)
                        .Select(a => a.Customer)
                        .FirstOrDefault(),
                Readings = x.Readings.Select(r => new
                {
                    r.Timestamp,
                    r.Value,
                    r.ReadingType,
                    r.IntervalSeconds,
                    r.QualityFlag,
                    r.DataOrigin
                }).ToList()
            })
            .SingleOrDefaultAsync(ct);
        if (row is null)
            return null;

        var curated = await Fields("MeteringPoint", id, ct);
        var readings = row.Readings
            .OrderBy(x => x.Timestamp)
            .ToArray();
        var interval = readings
            .Where(x => x.IntervalSeconds > 0)
            .Select(x => x.IntervalSeconds)
            .Distinct()
            .ToArray();
        var readingTypes = readings
            .Select(x => x.ReadingType)
            .Distinct()
            .ToArray();
        var readingType = readingTypes.Length == 1
            ? readingTypes[0]
            : (MeterReadingType?)null;
        var annual = CanonicalAnnualValue.Evaluate(
            readings.Select(x => (x.Timestamp, x.Value)).ToArray(),
            readingType);
        var start = readings.Length == 0
            ? (DateTime?)null
            : readings[0].Timestamp;
        var end = readings.Length == 0
            ? (DateTime?)null
            : readings[^1].Timestamp;
        var actual = readings.LongLength;
        var fixedInterval = interval.Length == 1
            ? interval[0]
            : null;
        long? expected = fixedInterval > 0 && start.HasValue && end.HasValue
            ? (long)Math.Floor(
                (end.Value - start.Value).TotalSeconds /
                fixedInterval.Value) + 1
            : null;
        decimal? completeness = expected > 0
            ? Math.Min(
                100m,
                decimal.Round(actual * 100m / expected.Value, 2))
            : null;
        var quantity = row.Meter.Quantity == MeterQuantity.Unknown
            ? (MeterQuantity?)null
            : row.Meter.Quantity;
        var unit = row.Meter.Unit == MeterUnit.Unknown
            ? (MeterUnit?)null
            : row.Meter.Unit;
        var medium = row.Meter.Medium == MeterMedium.Unknown
            ? (MeterMedium?)null
            : row.Meter.Medium;
        var quality = Quality(
            [
                row.Meter.MeterNumber,
                medium?.ToString(),
                quantity?.ToString(),
                unit?.ToString(),
                start?.ToString("O"),
                fixedInterval?.ToString()
            ],
            curated);
        var summary = new CanonicalReadingSummary(
            actual,
            start,
            end,
            unit,
            readingType,
            quantity,
            fixedInterval,
            readings.LongCount(x =>
                x.QualityFlag is DataQuality.Invalid or DataQuality.Missing),
            readings.LongCount(x => x.QualityFlag == DataQuality.Estimated),
            readings.LongCount(x =>
                x.QualityFlag == DataQuality.Interpolated),
            readings.LongCount(x =>
                x.QualityFlag is DataQuality.Measured or
                    DataQuality.Validated),
            readings.LongCount(x =>
                x.QualityFlag == DataQuality.Calculated),
            completeness,
            annual.Value,
            annual.Status)
        {
            AnnualValueReferenceYear =
                annual.Status == AnnualValueStatus.CompleteYear
                    ? start?.Year
                    : null
        };

        return new(
            row.Meter.Id,
            row.Meter.MeterNumber,
            row.Meter.Name,
            row.Building?.Id,
            row.Building?.BuildingNumber,
            row.Building?.Name,
            row.Customer?.Id,
            row.Customer?.Name,
            medium,
            row.Meter.Direction == MeterDirection.Unknown
                ? null
                : row.Meter.Direction,
            quantity,
            unit,
            Curated(curated, "UsageType"),
            row.Meter.IsActive,
            summary,
            quality,
            Suitability(
                row.Meter.MeterNumber,
                unit,
                fixedInterval),
            await Version("MeteringPoint", id, ct))
        {
            CustomerNumber = row.Customer?.CustomerNumber,
            ExternalIdentifier = row.Meter.ExternalIdentifier,
            MeterType = row.Meter.Type.ToString(),
            ValidFrom = row.Meter.CommissionedAt,
            ValidTo = row.Meter.DecommissionedAt,
            ReadingValues = readings.Select(x =>
                new CanonicalMeterReading(
                    x.Timestamp,
                    x.Value,
                    unit,
                    x.ReadingType,
                    x.IntervalSeconds,
                    x.QualityFlag.ToString(),
                    x.DataOrigin.ToString(),
                    x.ReadingType == MeterReadingType.Calculated ||
                    x.QualityFlag == DataQuality.Calculated))
                .ToArray()
        };
    }

    public async Task<CanonicalSnapshotSet> GetPortfolio(
        CancellationToken ct)
    {
        var customerIds = await scope.ApplyCustomerScope(db.Customers)
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(ct);
        var buildingIds = await scope.ApplyBuildingScope(db.Buildings)
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(ct);
        var meterIds = await scope.ApplyMeterScope(db.Meters)
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(ct);
        var customers = new List<CustomerCanonicalSnapshot>();
        var buildings = new List<BuildingCanonicalSnapshot>();
        var meters = new List<MeterCanonicalSnapshot>();
        foreach (var id in customerIds)
            if (await GetCustomer(id, ct) is { } value)
                customers.Add(value);
        foreach (var id in buildingIds)
            if (await GetBuilding(id, ct) is { } value)
                buildings.Add(value);
        foreach (var id in meterIds)
            if (await GetMeter(id, ct) is { } value)
                meters.Add(value);

        var systems = await db.EnergySystems.AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.Type,
                x.RatedPowerKw,
                x.CommissionedAt,
                x.DecommissionedAt,
                BuildingId = x.BuildingAssignments
                    .Where(a => a.ValidTo == null)
                    .Select(a => (Guid?)a.BuildingId)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);
        var energySystems = systems.Select(x =>
        {
            var quality = Quality(
                [
                    x.Type == EnergySystemType.Unknown
                        ? null
                        : x.Type.ToString(),
                    Invariant(x.RatedPowerKw)
                ],
                EmptyFields);
            return new EnergySystemCanonicalSnapshot(
                x.Id,
                x.Type == EnergySystemType.Unknown
                    ? null
                    : x.Type.ToString(),
                null,
                null,
                x.RatedPowerKw,
                x.CommissionedAt?.Year,
                x.BuildingId,
                quality,
                Suitability(x.Type, x.RatedPowerKw),
                TechnicalVersion("EnergySystem", x.Id))
            {
                ValidFrom = x.CommissionedAt,
                ValidTo = x.DecommissionedAt
            };
        }).ToArray();
        return new(customers, buildings, meters, energySystems);
    }

    private async Task<IReadOnlyDictionary<string, CuratedFieldValue>> Fields(
        string entity,
        Guid id,
        CancellationToken ct) =>
        await db.CuratedFieldValues
            .AsNoTracking()
            .Where(x =>
                x.EntityType == entity &&
                x.EntityId == id &&
                x.ValidToUtc == null &&
                x.Confirmed)
            .GroupBy(x => x.FieldName)
            .Select(group => group
                .OrderByDescending(x => x.ValidFromUtc)
                .First())
            .ToDictionaryAsync(x => x.FieldName, ct);

    private async Task<CanonicalVersion> Version(
        string entity,
        Guid id,
        CancellationToken ct)
    {
        var version = await db.GoldProfileVersions
            .AsNoTracking()
            .Where(x =>
                x.EntityType == entity &&
                x.EntityId == id &&
                x.IsCurrent)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new
            {
                x.Id,
                x.VersionNumber,
                x.CreatedAtUtc,
                x.ReleaseStatus
            })
            .FirstOrDefaultAsync(ct);
        return version is null
            ? TechnicalVersion(entity, id)
            : new(
                version.Id,
                version.VersionNumber,
                version.CreatedAtUtc,
                "GoldProfileVersion",
                version.ReleaseStatus);
    }

    private CanonicalVersion TechnicalVersion(string entity, Guid id) =>
        new(
            DeterministicId(entity, id),
            1,
            clock.GetUtcNow().UtcDateTime,
            "RelationalProjection",
            GoldProfileReleaseStatus.Draft);

    private static Guid DeterministicId(string entity, Guid id)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"{entity}:{id:D}"));
        return new Guid(bytes[..16]);
    }

    private static string? Curated(
        IReadOnlyDictionary<string, CuratedFieldValue> fields,
        string name) =>
        fields.TryGetValue(name, out var value)
            ? Clean(value.NormalizedValue)
            : null;

    private static SnapshotQuality Quality(
        IReadOnlyCollection<string?> required,
        IReadOnlyDictionary<string, CuratedFieldValue> curated)
    {
        var total = Math.Max(1, required.Count);
        var present = required.Count(value =>
            !string.IsNullOrWhiteSpace(value));
        var completeness = present * 100 / total;
        var curation = curated.Count == 0
            ? 0
            : curated.Values.Count(x =>
                x.Confirmed &&
                x.MaturityLevel == DataMaturityLevel.Gold) *
                100 / curated.Count;
        var level = completeness < 60
            ? DataMaturityLevel.Bronze
            : completeness == 100 &&
              curated.Count > 0 &&
              curation == 100
                ? DataMaturityLevel.Gold
                : DataMaturityLevel.Silver;
        return new(level, completeness, 100, 100, curation);
    }

    private static SnapshotSuitability Suitability(
        params object?[] required)
    {
        var suitable = required.All(x => x switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => true
        });
        var status = suitable
            ? SuitabilityStatus.Suitable
            : SuitabilityStatus.NotSuitable;
        return new(status, status, status, status);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal? Decimal(string? value) =>
        decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? parsed
            : null;

    private static int? Integer(string? value) =>
        int.TryParse(value, out var parsed) ? parsed : null;

    private static string? Invariant(decimal? value) =>
        value?.ToString(CultureInfo.InvariantCulture);

    private static readonly IReadOnlyDictionary<string, CuratedFieldValue>
        EmptyFields = new Dictionary<string, CuratedFieldValue>();
}
