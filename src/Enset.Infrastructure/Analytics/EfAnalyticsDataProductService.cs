using Enset.Application.Analytics;
using Enset.Domain.Data;
using Enset.Domain.Energy;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.Analytics;

public sealed class EfAnalyticsDataProductService : IAnalyticsDataProductService
{
    private readonly EnsetDbContext _db;
    private readonly TimeProvider _timeProvider;

    public EfAnalyticsDataProductService(EnsetDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<PortfolioSummaryDataProduct> GetPortfolioSummaryAsync(
        CancellationToken cancellationToken)
    {
        var customers = await _db.Customers.AsNoTracking()
            .CountAsync(cancellationToken);
        var buildings = await _db.Buildings.AsNoTracking()
            .CountAsync(cancellationToken);
        var meters = await _db.Meters.AsNoTracking()
            .CountAsync(cancellationToken);
        var documents = await _db.Documents.AsNoTracking()
            .CountAsync(cancellationToken);
        var energySystems = await _db.EnergySystems.AsNoTracking()
            .CountAsync(cancellationToken);

        return new(customers, buildings, meters, documents, energySystems, UtcNow);
    }

    public async Task<RegionalBuildingDistributionDataProduct>
        GetRegionalBuildingDistributionAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.BuildingVersions.AsNoTracking()
            .Where(version => version.ValidTo == null && version.Address != null)
            .GroupBy(version => new
            {
                State = version.Address!.Municipality == null
                    ? null
                    : version.Address.Municipality.District.State.Name,
                District = version.Address.Municipality == null
                    ? null
                    : version.Address.Municipality.District.Name,
                Municipality = version.Address.Municipality == null
                    ? null
                    : version.Address.Municipality.Name,
                PostalCode = version.Address.PostalCodeArea == null
                    ? null
                    : version.Address.PostalCodeArea.Code
            })
            .Select(group => new
            {
                group.Key.State,
                group.Key.District,
                group.Key.Municipality,
                group.Key.PostalCode,
                BuildingCount = group.Select(x => x.BuildingId).Distinct().Count(),
                CustomerCount = group.SelectMany(x => x.Building.CustomerAssignments)
                    .Where(x => x.ValidTo == null)
                    .Select(x => x.CustomerId).Distinct().Count(),
                MeterCount = group.SelectMany(x => x.Building.Meters)
                    .Select(x => x.Id).Distinct().Count()
            })
            .ToListAsync(cancellationToken);

        return new(rows.Select(x => new RegionalBuildingDistributionItem(
            x.State, x.District, x.Municipality, x.PostalCode,
            x.BuildingCount, x.CustomerCount, x.MeterCount))
            .ToArray(), UtcNow);
    }

    public async Task<ElectricityPortfolioLoadProfileDataProduct>
        GetPortfolioLoadProfileAsync(AnalyticsQuery query, CancellationToken cancellationToken)
    {
        ValidateQuery(query);
        var from = new DateTime(query.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddYears(1);
        var readings = await FilterReadings(query, from, to)
            .Where(x => x.Meter.Medium == MeterMedium.Electricity
                && x.Meter.Quantity == MeterQuantity.Power
                && (x.Meter.Unit == MeterUnit.W
                    || x.Meter.Unit == MeterUnit.KW
                    || x.Meter.Unit == MeterUnit.MW))
            .Select(x => new ReadingRow(
                x.MeterId, x.Timestamp, x.Value, x.Meter.Unit, x.IntervalSeconds))
            .ToListAsync(cancellationToken);

        var points = readings
            .GroupBy(x => Bucket(x.Timestamp, query.Aggregation))
            .OrderBy(x => x.Key)
            .Select(group => new TimeSeriesPoint(
                group.Key, group.Sum(x => ConvertPowerToKw(x.Value, x.Unit))))
            .ToArray();

        return new(query.Year, query.Aggregation, "Power",
            points.Length == 0 ? null : "kW", points,
            Coverage(readings.Select(x => x.MeterId).Distinct().Count(),
                await ElectricityMeterCount(
                    query, MeterQuantity.Power, cancellationToken)),
            readings.Count == 0 ? null : readings.Max(x => x.Timestamp), UtcNow);
    }

    public async Task<MonthlyElectricityConsumptionDataProduct>
        GetMonthlyElectricityConsumptionAsync(
            AnalyticsQuery query, CancellationToken cancellationToken)
    {
        ValidateQuery(query);
        var from = new DateTime(query.Year - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddYears(2);
        var readings = await FilterReadings(query, from, to)
            .Where(x => x.Meter.Medium == MeterMedium.Electricity
                && x.Meter.Quantity == MeterQuantity.Energy
                && x.ReadingType == MeterReadingType.IntervalValue
                && (x.Meter.Unit == MeterUnit.Wh
                    || x.Meter.Unit == MeterUnit.KWh
                    || x.Meter.Unit == MeterUnit.MWh))
            .Select(x => new ReadingRow(
                x.MeterId, x.Timestamp, x.Value, x.Meter.Unit, x.IntervalSeconds))
            .ToListAsync(cancellationToken);

        decimal? Sum(int year, int month)
        {
            var values = readings.Where(x => x.Timestamp.Year == year
                && x.Timestamp.Month == month).ToArray();
            return values.Length == 0
                ? null
                : values.Sum(x => ConvertEnergyToKwh(x.Value, x.Unit));
        }

        var months = Enumerable.Range(1, 12)
            .Select(month => new MonthlyConsumptionPoint(
                month, Sum(query.Year, month), Sum(query.Year - 1, month)))
            .ToArray();
        var values = months.Where(x => x.Value.HasValue).Select(x => x.Value!.Value);

        return new(query.Year, "Energy",
            months.Any(x => x.Value.HasValue) ? "kWh" : null,
            months, values.Any() ? values.Sum() : null,
            Coverage(
                readings.Where(x => x.Timestamp.Year == query.Year)
                    .Select(x => x.MeterId).Distinct().Count(),
                await ElectricityMeterCount(
                    query, MeterQuantity.Energy, cancellationToken)),
            UtcNow);
    }

    public async Task<MeteringCoverageSummaryDataProduct> GetMeteringCoverageAsync(
        CancellationToken cancellationToken)
    {
        var buildingCount = await _db.Buildings.CountAsync(cancellationToken);
        var withReadings = await _db.Buildings
            .CountAsync(x => x.Meters.Any(m => m.Readings.Any()), cancellationToken);
        var active = await _db.Meters.CountAsync(x => x.IsActive, cancellationToken);
        var inactive = await _db.Meters.CountAsync(x => !x.IsActive, cancellationToken);
        var without = await _db.Meters.CountAsync(
            x => !x.Readings.Any(), cancellationToken);
        var last = await _db.MeterReadings.MaxAsync(
            x => (DateTime?)x.Timestamp, cancellationToken);

        return new(withReadings, buildingCount - withReadings, active, inactive,
            without, last, Coverage(withReadings, buildingCount), UtcNow);
    }

    public async Task<DataQualitySummaryDataProduct> GetDataQualityAsync(
        CancellationToken cancellationToken)
    {
        var staleBefore = UtcNow.AddDays(-30);
        var totalMeters = await _db.Meters.CountAsync(cancellationToken);
        var missingAssignments = await _db.Meters.CountAsync(
            x => x.BuildingId == null && x.EnergySystemId == null, cancellationToken);
        var stale = await _db.Meters.CountAsync(x =>
            !x.Readings.Any() || x.Readings.Max(r => r.Timestamp) < staleBefore,
            cancellationToken);
        var incomplete = await _db.Buildings.CountAsync(
            x => string.IsNullOrEmpty(x.Name) || string.IsNullOrEmpty(x.BuildingNumber),
            cancellationToken);
        var invalidUnits = await _db.Meters.CountAsync(
            x => x.Unit == MeterUnit.Unknown, cancellationToken);
        var invalidReadings = await _db.MeterReadings.CountAsync(
            x => x.QualityFlag == DataQuality.Invalid, cancellationToken);
        var critical = missingAssignments + invalidUnits + invalidReadings;
        decimal? score = null;

        return new(score, missingAssignments, stale, incomplete, invalidUnits,
            null, null, critical, UtcNow);
    }

    public async Task<EnergyPortfolioStructureDataProduct> GetEnergyPortfolioAsync(
        CancellationToken cancellationToken)
    {
        async Task<int> Buildings(MeterMedium medium) => await _db.Buildings
            .CountAsync(x => x.Meters.Any(m => m.Medium == medium), cancellationToken);

        var systems = await _db.EnergySystems.AsNoTracking()
            .GroupBy(x => x.Type)
            .Select(x => new NamedCount(x.Key.ToString(), x.Count()))
            .ToListAsync(cancellationToken);
        var meters = await _db.Meters.AsNoTracking()
            .GroupBy(x => x.Medium)
            .Select(x => new NamedCount(x.Key.ToString(), x.Count()))
            .ToListAsync(cancellationToken);

        return new(await Buildings(MeterMedium.Electricity),
            await Buildings(MeterMedium.Heat), await Buildings(MeterMedium.Gas),
            await Buildings(MeterMedium.Water), systems.Sum(x => x.Count),
            systems, meters, UtcNow);
    }

    public async Task<ManagementWarningsDataProduct> GetWarningsAsync(
        CancellationToken cancellationToken)
    {
        var quality = await GetDataQualityAsync(cancellationToken);
        var warnings = new List<ManagementWarning>();
        AddWarning(warnings, "MISSING_ASSIGNMENT", "Critical",
            "Zähler ohne Gebäude- oder Anlagenzuordnung.", quality.MissingAssignments);
        AddWarning(warnings, "STALE_READING", "Warning",
            "Zähler ohne Messwert innerhalb der letzten 30 Tage.", quality.StaleReadings);
        AddWarning(warnings, "INVALID_UNIT", "Critical",
            "Zähler ohne gültige Einheit.", quality.InvalidUnits);
        var totalAffectedCount = warnings.Sum(w => w.AffectedCount);
        return new(warnings, totalAffectedCount, UtcNow);
    }

    public async Task<ManagementWarningDetailsDataProduct> GetWarningsDetailsAsync(
        int page,
        int pageSize,
        string? severity,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var now = UtcNow;
        var staleThreshold = now.AddDays(-30);

        var missingAssignments = _db.Meters.AsNoTracking()
            .Where(x => x.BuildingId == null && x.EnergySystemId == null)
            .Select(x => new WarningRow(
                "MISSING_ASSIGNMENT",
                "Critical",
                "Fehlende Zuordnung",
                "Meter",
                x.Id,
                x.MeterNumber,
                x.BuildingId,
                x.Building != null ? x.Building.Name : null,
                x.Id,
                x.MeterNumber,
                "Zähler ohne Gebäude- oder Anlagenzuordnung.",
                "Der Zähler ist aktuell keinem Gebäude oder keiner Energieanlage zugeordnet.",
                "Zähler einer Anlage oder einem Gebäude zuordnen.",
                "Offen",
                now));

        var staleReadings = _db.Meters.AsNoTracking()
            .Where(x => !x.Readings.Any() || x.Readings.Max(r => r.Timestamp) < staleThreshold)
            .Select(x => new WarningRow(
                "STALE_READING",
                "Warning",
                "Veraltete Messwerte",
                "Meter",
                x.Id,
                x.MeterNumber,
                x.BuildingId,
                x.Building != null ? x.Building.Name : null,
                x.Id,
                x.MeterNumber,
                "Zähler ohne Messwert innerhalb der letzten 30 Tage.",
                "Der letzte verfügbare Messwert ist älter als 30 Tage oder fehlt vollständig.",
                "Aktuelle Messwerte nachpflegen oder Gerät prüfen.",
                "Offen",
                x.Readings.Max(r => (DateTime?)r.Timestamp) ?? now));

        var invalidUnits = _db.Meters.AsNoTracking()
            .Where(x => x.Unit == MeterUnit.Unknown)
            .Select(x => new WarningRow(
                "INVALID_UNIT",
                "Critical",
                "Ungültige Einheit",
                "Meter",
                x.Id,
                x.MeterNumber,
                x.BuildingId,
                x.Building != null ? x.Building.Name : null,
                x.Id,
                x.MeterNumber,
                "Zähler ohne gültige Einheit.",
                "Die Einheit des Zählers ist unbekannt oder nicht validiert.",
                "Die Einheit des Zählers validieren oder korrigieren.",
                "Offen",
                now));

        var warnings = missingAssignments
            .Concat(staleReadings)
            .Concat(invalidUnits);

        if (!string.IsNullOrWhiteSpace(severity))
        {
            warnings = warnings.Where(x => x.Severity == severity);
        }

        var total = await warnings.CountAsync(cancellationToken);
        var items = await warnings
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.WarningType)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        var details = items.Select(x => new ManagementWarningDetail(
            x.Code,
            x.Severity,
            x.WarningType,
            x.EntityType,
            x.EntityId,
            x.Entity,
            x.BuildingId,
            x.Building,
            x.MeterId,
            x.Meter,
            x.Description,
            x.Cause,
            x.Recommendation,
            x.Status,
            x.OccurredAt)).ToArray();

        return new(details, total, page, pageSize, now);
    }

    public async Task<EnergyConsumptionByLocationDataProduct>
        GetConsumptionByLocationAsync(
            AnalyticsQuery query, int limit, CancellationToken cancellationToken)
    {
        ValidateQuery(query);
        limit = Math.Clamp(limit, 1, 100);
        var from = new DateTime(query.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var rows = await FilterReadings(query, from, from.AddYears(1))
            .Where(x => x.Meter.BuildingId != null
                && x.Meter.Quantity == MeterQuantity.Energy
                && x.ReadingType == MeterReadingType.IntervalValue
                && (x.Meter.Unit == MeterUnit.Wh
                    || x.Meter.Unit == MeterUnit.KWh
                    || x.Meter.Unit == MeterUnit.MWh))
            .Select(x => new
            {
                BuildingId = x.Meter.BuildingId!.Value,
                Building = x.Meter.Building!.Name,
                Municipality = x.Meter.Building.Versions
                    .Where(v => v.ValidTo == null && v.Address != null
                        && v.Address.Municipality != null)
                    .Select(v => v.Address!.Municipality!.Name)
                    .FirstOrDefault(),
                PostalCode = x.Meter.Building.Versions
                    .Where(v => v.ValidTo == null && v.Address != null
                        && v.Address.PostalCodeArea != null)
                    .Select(v => v.Address!.PostalCodeArea!.Code)
                    .FirstOrDefault(),
                x.MeterId,
                x.Value,
                x.Meter.Unit
            })
            .ToListAsync(cancellationToken);

        var locations = rows.GroupBy(x => new
            { x.BuildingId, x.Building, x.Municipality, x.PostalCode })
            .Select(group => new ConsumptionByLocationItem(
                group.Key.BuildingId, group.Key.Building,
                group.Key.Municipality, group.Key.PostalCode,
                group.Sum(x => ConvertEnergyToKwh(x.Value, x.Unit)), "kWh",
                group.Select(x => x.MeterId).Distinct().Count(), null))
            .OrderByDescending(x => x.Consumption).Take(limit).ToArray();
        return new(query.Year, locations, UtcNow);
    }

    public Task<EnergyConsumptionByUsageTypeDataProduct>
        GetConsumptionByUsageTypeAsync(
            AnalyticsQuery query, CancellationToken cancellationToken)
    {
        ValidateQuery(query);
        // Meter currently has no canonical UsageType assignment.
        return Task.FromResult(new EnergyConsumptionByUsageTypeDataProduct(
            query.Year, [], UtcNow));
    }

    public async Task<TopEnergySystemsByConsumptionDataProduct>
        GetTopEnergySystemsAsync(
            AnalyticsQuery query, int limit, CancellationToken cancellationToken)
    {
        ValidateQuery(query);
        limit = Math.Clamp(limit, 1, 100);
        var from = new DateTime(query.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var rows = await FilterReadings(query, from, from.AddYears(1))
            .Where(x => x.Meter.EnergySystemId != null
                && x.Meter.Quantity == MeterQuantity.Energy
                && x.ReadingType == MeterReadingType.IntervalValue
                && (x.Meter.Unit == MeterUnit.Wh
                    || x.Meter.Unit == MeterUnit.KWh
                    || x.Meter.Unit == MeterUnit.MWh))
            .Select(x => new
            {
                EnergySystemId = x.Meter.EnergySystemId!.Value,
                EnergySystem = x.Meter.EnergySystem!.Name,
                SystemType = x.Meter.EnergySystem.Type,
                x.Value,
                x.Meter.Unit
            }).ToListAsync(cancellationToken);
        var totals = rows.GroupBy(x => new
            { x.EnergySystemId, x.EnergySystem, x.SystemType })
            .Select(x => new
            {
                x.Key,
                Consumption = x.Sum(v => ConvertEnergyToKwh(v.Value, v.Unit))
            }).ToArray();
        var total = totals.Sum(x => x.Consumption);
        var result = totals.OrderByDescending(x => x.Consumption).Take(limit)
            .Select(x => new TopEnergySystemItem(
                x.Key.EnergySystemId, x.Key.EnergySystem, null,
                x.Key.SystemType.ToString(), null, x.Consumption,
                total == 0 ? 0 : x.Consumption / total * 100m, "kWh", null))
            .ToArray();
        return new(query.Year, result, UtcNow);
    }

    public async Task<EnergyConsumptionByCarrierDataProduct>
        GetConsumptionByCarrierAsync(
            AnalyticsQuery query, CancellationToken cancellationToken)
    {
        ValidateQuery(query);
        var from = new DateTime(query.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var rows = await FilterReadings(query, from, from.AddYears(1))
            .Where(x => x.Meter.Quantity == MeterQuantity.Energy
                && x.ReadingType == MeterReadingType.IntervalValue
                && (x.Meter.Unit == MeterUnit.Wh
                    || x.Meter.Unit == MeterUnit.KWh
                    || x.Meter.Unit == MeterUnit.MWh))
            .Select(x => new
            {
                x.Meter.Medium,
                x.MeterId,
                x.Meter.BuildingId,
                x.Value,
                x.Meter.Unit
            }).ToListAsync(cancellationToken);
        var grouped = rows.GroupBy(x => x.Medium)
            .Select(x => new
            {
                Carrier = x.Key.ToString(),
                Consumption = x.Sum(v => ConvertEnergyToKwh(v.Value, v.Unit)),
                MeterCount = x.Select(v => v.MeterId).Distinct().Count(),
                BuildingCount = x.Where(v => v.BuildingId != null)
                    .Select(v => v.BuildingId).Distinct().Count()
            }).ToArray();
        var total = grouped.Sum(x => x.Consumption);
        var carriers = grouped.Select(x => new ConsumptionByCarrierItem(
            x.Carrier, x.Consumption,
            total == 0 ? 0 : x.Consumption / total * 100m,
            "kWh", x.BuildingCount, x.MeterCount)).ToArray();
        return new(query.Year, "kWh", carriers, UtcNow);
    }

    private IQueryable<MeterReading> FilterReadings(
        AnalyticsQuery query, DateTime from, DateTime to)
    {
        var readings = _db.MeterReadings.AsNoTracking()
            .Where(x => x.Timestamp >= from && x.Timestamp < to
                && x.QualityFlag != DataQuality.Invalid
                && x.QualityFlag != DataQuality.Missing);
        if (query.MeterId.HasValue)
            readings = readings.Where(x => x.MeterId == query.MeterId);
        if (query.BuildingId.HasValue)
            readings = readings.Where(x => x.Meter.BuildingId == query.BuildingId);
        if (query.CustomerId.HasValue)
            readings = readings.Where(x => x.Meter.Building!.CustomerAssignments
                .Any(a => a.CustomerId == query.CustomerId && a.ValidTo == null));
        if (!string.IsNullOrWhiteSpace(query.PostalCode))
            readings = readings.Where(x => x.Meter.Building!.Versions.Any(v =>
                v.ValidTo == null && v.Address!.PostalCodeArea!.Code == query.PostalCode));
        if (!string.IsNullOrWhiteSpace(query.Region))
            readings = readings.Where(x => x.Meter.Building!.Versions.Any(v =>
                v.ValidTo == null && v.Address!.Municipality!.District.State.Name == query.Region));
        return readings;
    }

    private async Task<int> ElectricityMeterCount(
        AnalyticsQuery query,
        MeterQuantity quantity,
        CancellationToken cancellationToken)
    {
        var meters = _db.Meters.AsNoTracking()
            .Where(x => x.Medium == MeterMedium.Electricity
                && x.Quantity == quantity);
        if (query.MeterId.HasValue) meters = meters.Where(x => x.Id == query.MeterId);
        if (query.BuildingId.HasValue)
            meters = meters.Where(x => x.BuildingId == query.BuildingId);
        if (query.CustomerId.HasValue)
            meters = meters.Where(x => x.Building!.CustomerAssignments
                .Any(a => a.CustomerId == query.CustomerId && a.ValidTo == null));
        return await meters.CountAsync(cancellationToken);
    }

    private static DateTime Bucket(DateTime timestamp, string aggregation) =>
        aggregation.ToLowerInvariant() switch
        {
            "quarterhour" => new DateTime(timestamp.Year, timestamp.Month,
                timestamp.Day, timestamp.Hour, timestamp.Minute / 15 * 15, 0,
                DateTimeKind.Utc),
            "day" => new DateTime(timestamp.Year, timestamp.Month, timestamp.Day,
                0, 0, 0, DateTimeKind.Utc),
            "week" => timestamp.Date.AddDays(
                -((7 + (int)timestamp.DayOfWeek - (int)DayOfWeek.Monday) % 7)),
            "month" => new DateTime(timestamp.Year, timestamp.Month, 1,
                0, 0, 0, DateTimeKind.Utc),
            _ => new DateTime(timestamp.Year, timestamp.Month, timestamp.Day,
                timestamp.Hour, 0, 0, DateTimeKind.Utc)
        };

    private static decimal ConvertPowerToKw(decimal value, MeterUnit unit) =>
        unit switch
        {
            MeterUnit.W => value / 1_000m,
            MeterUnit.KW => value,
            MeterUnit.MW => value * 1_000m,
            _ => throw new InvalidOperationException("Unsupported power unit.")
        };

    private static decimal ConvertEnergyToKwh(decimal value, MeterUnit unit) =>
        unit switch
        {
            MeterUnit.Wh => value / 1_000m,
            MeterUnit.KWh => value,
            MeterUnit.MWh => value * 1_000m,
            _ => throw new InvalidOperationException("Unsupported energy unit.")
        };

    private static decimal Coverage(int covered, int total) =>
        total == 0 ? 0 : decimal.Round(covered * 100m / total, 2);

    private sealed record WarningRow(
        string Code,
        string Severity,
        string WarningType,
        string? EntityType,
        Guid? EntityId,
        string? Entity,
        Guid? BuildingId,
        string? Building,
        Guid? MeterId,
        string? Meter,
        string Description,
        string Cause,
        string Recommendation,
        string Status,
        DateTime OccurredAt);

    private static void AddWarning(
        ICollection<ManagementWarning> warnings,
        string code,
        string severity,
        string message,
        int count)
    {
        if (count > 0) warnings.Add(new(code, severity, message, count));
    }

    private static void ValidateQuery(AnalyticsQuery query)
    {
        if (query.Year is < 1900 or > 2200)
            throw new ArgumentOutOfRangeException(nameof(query.Year));
        var allowed = new[] { "QuarterHour", "Hour", "Day", "Week", "Month" };
        if (!allowed.Contains(query.Aggregation, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Unsupported aggregation.", nameof(query));
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    private sealed record ReadingRow(
        Guid MeterId,
        DateTime Timestamp,
        decimal Value,
        MeterUnit Unit,
        int? IntervalSeconds);
}
