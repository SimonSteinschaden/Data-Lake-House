using Enset.Application.CanonicalSnapshots;
using Enset.Application.ObjectAnalytics;
using Enset.Domain.Curation;
using Enset.Domain.Energy;

namespace Enset.Infrastructure.ObjectAnalytics;

public sealed class CanonicalObjectAnalyticsService(
    ICanonicalSnapshotReader snapshots,
    TimeProvider timeProvider)
    : IObjectAnalyticsService
{
    public async Task<ObjectSearchResult> Search(
        ObjectSearchQuery query,
        CancellationToken cancellationToken)
    {
        Validate(query);
        var portfolio = await snapshots.GetPortfolio(cancellationToken);
        var metersByBuilding = portfolio.Meters
            .Where(x => x.BuildingId.HasValue)
            .GroupBy(x => x.BuildingId!.Value)
            .ToDictionary(x => x.Key, x => x.ToArray());
        var filtered = portfolio.Buildings.Where(building =>
        {
            var meters = metersByBuilding.GetValueOrDefault(
                building.BuildingId, []);
            var searchable = string.Join(" ", new[]
            {
                building.Name,
                building.BuildingNumber,
                building.CustomerName,
                building.MunicipalityName,
                building.Street,
                building.HouseNumber,
                building.PostalCode,
                building.City,
                string.Join(" ", meters.Select(x => x.MeterNumber))
            });
            return (string.IsNullOrWhiteSpace(query.Search) ||
                    searchable.Contains(
                        query.Search.Trim(),
                        StringComparison.OrdinalIgnoreCase)) &&
                   (query.CustomerId is null ||
                    building.CustomerId == query.CustomerId) &&
                   (string.IsNullOrWhiteSpace(query.BuildingType) ||
                    string.Equals(
                        building.BuildingType,
                        query.BuildingType,
                        StringComparison.OrdinalIgnoreCase)) &&
                   (string.IsNullOrWhiteSpace(query.QualityLevel) ||
                    string.Equals(
                        building.Quality.Level.ToString(),
                        query.QualityLevel,
                        StringComparison.OrdinalIgnoreCase)) &&
                   (string.IsNullOrWhiteSpace(query.EnergyCarrier) ||
                    meters.Any(x => string.Equals(
                        x.Medium?.ToString(),
                        query.EnergyCarrier,
                        StringComparison.OrdinalIgnoreCase)));
        }).OrderBy(x => x.Name).ThenBy(x => x.BuildingNumber).ToArray();

        var items = filtered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(building =>
            {
                var meters = metersByBuilding.GetValueOrDefault(
                    building.BuildingId, []);
                return new ObjectSearchItem(
                    building.BuildingId,
                    building.BuildingNumber,
                    building.Name,
                    building.CustomerId,
                    building.CustomerName,
                    building.BuildingType,
                    building.MunicipalityName,
                    Address(building),
                    meters.Select(x => x.MeterNumber).ToArray(),
                    building.Quality,
                    building.Suitability);
            }).ToArray();
        return new(items, filtered.Length, query.Page, query.PageSize);
    }

    public async Task<ObjectAnalyticsProduct?> Analyze(
        Guid buildingId,
        ObjectSearchQuery query,
        CancellationToken cancellationToken)
    {
        Validate(query);
        var portfolio = await snapshots.GetPortfolio(cancellationToken);
        var building = portfolio.Buildings.SingleOrDefault(
            x => x.BuildingId == buildingId);
        if (building is null)
            return null;
        var meters = portfolio.Meters
            .Where(x => x.BuildingId == buildingId)
            .Where(x => string.IsNullOrWhiteSpace(query.EnergyCarrier) ||
                string.Equals(
                    x.Medium?.ToString(),
                    query.EnergyCarrier,
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var readings = meters.SelectMany(meter =>
                meter.ReadingValues
                    .Where(x => x.Timestamp >= query.FromUtc &&
                                x.Timestamp < query.ToUtc)
                    .Select(x => new Reading(meter, x)))
            .ToArray();
        var energy = readings.Where(IsEnergy)
            .Select(x => new
            {
                x.Meter,
                x.Value.Timestamp,
                Value = ToKwh(x.Value.Value, x.Meter.Unit)
            })
            .Where(x => x.Value.HasValue)
            .Select(x => new ConvertedReading(
                x.Meter, x.Timestamp, x.Value!.Value))
            .ToArray();
        var consumption = energy
            .Where(x => x.Meter.Direction != MeterDirection.Production)
            .ToArray();
        var production = energy
            .Where(x => x.Meter.Direction == MeterDirection.Production)
            .ToArray();
        var power = readings.Where(IsPower)
            .Select(x => new
            {
                x.Meter,
                x.Value.Timestamp,
                Value = ToKw(x.Value.Value, x.Meter.Unit)
            })
            .Where(x => x.Value.HasValue)
            .Select(x => new ConvertedReading(
                x.Meter, x.Timestamp, x.Value!.Value))
            .ToArray();
        var peak = power.OrderByDescending(x => x.Value).FirstOrDefault();
        var total = consumption.Sum(x => x.Value);
        var prior = PriorYear(meters, query, total);
        var benchmark = Benchmark(portfolio, building, query, total);
        var warnings = Warnings(building, meters);
        var anomalies = Anomalies(consumption, power, benchmark);
        var systems = portfolio.EnergySystems
            .Where(x => x.BuildingId == buildingId)
            .Select(x => new EnergySystemAnalyticsItem(
                x.EnergySystemId,
                x.Type,
                x.EnergyCarrier,
                x.InstalledPower,
                x.ConstructionYear,
                x.ValidTo is null ? "Active" : "Inactive"))
            .ToArray();
        var area = building.HeatedArea ??
                   building.ConditionedFloorArea ??
                   building.NetFloorArea ??
                   building.GrossFloorArea;

        return new ObjectAnalyticsProduct(
            building.BuildingId,
            building.BuildingNumber,
            building.Name,
            query.FromUtc,
            query.ToUtc,
            building.Quality,
            building.Suitability,
            Value(total, "kWh", building, "Meter Consumption Summary",
                "Sum of valid interval energy consumption."),
            Value(Sum(consumption, MeterMedium.Electricity), "kWh",
                building, "Meter Consumption Summary",
                "Sum where Medium=Electricity."),
            Value(Sum(consumption, MeterMedium.Heat), "kWh",
                building, "Meter Consumption Summary",
                "Sum where Medium=Heat."),
            Value(production.Length == 0 ? null : production.Sum(x => x.Value),
                "kWh", building, "Annual Energy Balance",
                "Sum where Direction=Production."),
            Unavailable("EUR", building,
                "No canonical tariff data is available."),
            Unavailable("kg CO2e", building,
                "No approved emission factor data product is available."),
            Value(peak?.Value, "kW", building, "Peak Load Profile",
                "Maximum canonical power reading in period."),
            peak?.Timestamp,
            Value(area > 0 ? total / area : null, "kWh/m²", building,
                "Building Energy Profile",
                "Consumption divided by canonical usable area."),
            Unavailable("kWh/user", building,
                "Canonical occupant count is unavailable."),
            Unavailable("%", building,
                "Consumption/production matching is unavailable."),
            Value(production.Length > 0 && total + production.Sum(x => x.Value) > 0
                    ? production.Sum(x => x.Value) /
                      (total + production.Sum(x => x.Value)) * 100m
                    : null,
                "%", building, "Annual Energy Balance",
                "Production divided by consumption plus production."),
            Series(consumption, x => new DateTime(
                x.Timestamp.Year, x.Timestamp.Month, 1,
                0, 0, 0, DateTimeKind.Utc)),
            Series(power, x => new DateTime(
                2000, 1, 1, x.Timestamp.Hour, 0, 0,
                DateTimeKind.Utc)),
            Series(power, x => new DateTime(
                2000, 1, 3 + (int)x.Timestamp.DayOfWeek,
                x.Timestamp.Hour, 0, 0, DateTimeKind.Utc)),
            consumption.GroupBy(x => x.Meter.Medium?.ToString() ?? "Unknown")
                .Select(x => new AnalyticsCategory(
                    x.Key, x.Sum(v => v.Value), "kWh"))
                .OrderByDescending(x => x.Value).ToArray(),
            [],
            systems,
            systems.Any(x => x.InstalledPower.HasValue)
                ? systems.Sum(x => x.InstalledPower ?? 0) : null,
            anomalies,
            warnings,
            Completeness(building, meters, systems),
            prior,
            benchmark,
            timeProvider.GetUtcNow().UtcDateTime);
    }

    private static AnalyticsValue Value(
        decimal? value,
        string unit,
        BuildingCanonicalSnapshot building,
        string source,
        string rule) =>
        new(value, unit, value.HasValue ? "Available" : "NotAvailable",
            source, rule, building.Quality, building.Suitability);

    private static AnalyticsValue Unavailable(
        string unit,
        BuildingCanonicalSnapshot building,
        string reason) =>
        Value(null, unit, building, "Not available", reason);

    private static decimal Sum(
        IEnumerable<ConvertedReading> values,
        MeterMedium medium) =>
        values.Where(x => x.Meter.Medium == medium).Sum(x => x.Value);

    private static IReadOnlyList<AnalyticsSeriesPoint> Series(
        IEnumerable<ConvertedReading> values,
        Func<ConvertedReading, DateTime> bucket) =>
        values.GroupBy(bucket).OrderBy(x => x.Key)
            .Select(x => new AnalyticsSeriesPoint(
                x.Key, x.Sum(v => v.Value))).ToArray();

    private static bool IsEnergy(Reading value) =>
        value.Meter.Quantity == MeterQuantity.Energy &&
        value.Value.ReadingType == MeterReadingType.IntervalValue;

    private static bool IsPower(Reading value) =>
        value.Meter.Quantity == MeterQuantity.Power;

    private static decimal? ToKwh(decimal value, MeterUnit? unit) =>
        unit switch
        {
            MeterUnit.Wh => value / 1000m,
            MeterUnit.KWh => value,
            MeterUnit.MWh => value * 1000m,
            _ => null
        };

    private static decimal? ToKw(decimal value, MeterUnit? unit) =>
        unit switch
        {
            MeterUnit.W => value / 1000m,
            MeterUnit.KW => value,
            MeterUnit.MW => value * 1000m,
            _ => null
        };

    private static PreviousYearComparison PriorYear(
        IReadOnlyList<MeterCanonicalSnapshot> meters,
        ObjectSearchQuery query,
        decimal current)
    {
        var from = query.FromUtc.AddYears(-1);
        var to = query.ToUtc.AddYears(-1);
        var previous = meters.SelectMany(meter =>
                meter.ReadingValues
                    .Where(x => x.Timestamp >= from && x.Timestamp < to)
                    .Select(x => new Reading(meter, x)))
            .Where(IsEnergy)
            .Where(x => x.Meter.Direction != MeterDirection.Production)
            .Select(x => ToKwh(x.Value.Value, x.Meter.Unit))
            .Where(x => x.HasValue).Sum(x => x!.Value);
        if (previous == 0)
            return new(current, null, null, null, "kWh", "NotAvailable");
        var delta = current - previous;
        return new(current, previous, delta,
            decimal.Round(delta / previous * 100m, 2),
            "kWh", "Available");
    }

    private static BenchmarkResult Benchmark(
        CanonicalSnapshotSet portfolio,
        BuildingCanonicalSnapshot building,
        ObjectSearchQuery query,
        decimal objectValue)
    {
        var candidates = portfolio.Buildings.Where(x =>
                x.BuildingId != building.BuildingId &&
                !string.IsNullOrWhiteSpace(building.BuildingType) &&
                string.Equals(x.BuildingType, building.BuildingType,
                    StringComparison.OrdinalIgnoreCase))
            .Select(candidate => portfolio.Meters
                .Where(x => x.BuildingId == candidate.BuildingId)
                .SelectMany(meter => meter.ReadingValues
                    .Where(x => x.Timestamp >= query.FromUtc &&
                                x.Timestamp < query.ToUtc)
                    .Where(x => x.ReadingType ==
                                MeterReadingType.IntervalValue)
                    .Select(x => ToKwh(x.Value, meter.Unit)))
                .Where(x => x.HasValue).Sum(x => x!.Value))
            .Where(x => x > 0).ToArray();
        return candidates.Length < 3
            ? new("NotAvailable", objectValue, null, "kWh",
                candidates.Length,
                "At least three buildings with the same canonical type.")
            : new("Available", objectValue,
                decimal.Round(candidates.Average(), 2), "kWh",
                candidates.Length,
                "Arithmetic mean of comparable canonical buildings.");
    }

    private static IReadOnlyList<AnalyticsWarning> Warnings(
        BuildingCanonicalSnapshot building,
        IReadOnlyList<MeterCanonicalSnapshot> meters)
    {
        var result = new List<AnalyticsWarning>();
        if (building.Quality.Level == DataMaturityLevel.Bronze)
            result.Add(new("LOW_QUALITY", "Warning",
                "Canonical building quality is Bronze.",
                "Warn when SnapshotQuality.Level is Bronze."));
        if (meters.Count == 0)
            result.Add(new("METER_MISSING", "Critical",
                "No canonical metering point is assigned.",
                "Warn when a building has no meter snapshot."));
        if (meters.Any(x => x.Medium is null or MeterMedium.Unknown))
            result.Add(new("CARRIER_MISSING", "Warning",
                "At least one energy carrier is unknown.",
                "Warn when Meter.Medium is Unknown."));
        if (meters.Any(x => x.Readings.AnnualValueStatus !=
                            AnnualValueStatus.CompleteYear))
            result.Add(new("ANNUAL_VALUE_MISSING", "Warning",
                "At least one complete annual value is unavailable.",
                "Warn unless AnnualValueStatus=CompleteYear."));
        return result;
    }

    private static IReadOnlyList<AnalyticsWarning> Anomalies(
        IReadOnlyList<ConvertedReading> consumption,
        IReadOnlyList<ConvertedReading> power,
        BenchmarkResult benchmark)
    {
        var result = new List<AnalyticsWarning>();
        if (benchmark.ComparisonValue > 0 &&
            benchmark.ObjectValue > benchmark.ComparisonValue * 1.5m)
            result.Add(new("HIGH_CONSUMPTION", "Warning",
                "Consumption exceeds the comparable-building mean by more than 50%.",
                "Object > 150% of benchmark mean; minimum sample is three."));
        if (benchmark.ComparisonValue > 0 &&
            benchmark.ObjectValue < benchmark.ComparisonValue * 0.5m)
            result.Add(new("LOW_CONSUMPTION", "Info",
                "Consumption is below 50% of the comparable-building mean.",
                "Object < 50% of benchmark mean; minimum sample is three."));
        if (power.Count >= 24)
        {
            var median = power.Select(x => x.Value).Order().ElementAt(
                power.Count / 2);
            if (median > 0 && power.Max(x => x.Value) > median * 3)
                result.Add(new("LOAD_PEAK", "Warning",
                    "Maximum load exceeds three times the median.",
                    "At least 24 power values; peak > 3 × median."));
        }
        if (consumption.Count == 0)
            result.Add(new("READINGS_MISSING", "Critical",
                "No valid energy readings exist in the selected period.",
                "No convertible interval-energy values in period."));
        return result;
    }

    private static IReadOnlyList<CompletenessIndicator> Completeness(
        BuildingCanonicalSnapshot building,
        IReadOnlyList<MeterCanonicalSnapshot> meters,
        IReadOnlyList<EnergySystemAnalyticsItem> systems) =>
        [
            Indicator("Messwerte", meters.Any(x =>
                x.Readings.CompletenessPercentage >= 95),
                meters.Any(x => x.Readings.MeasurementCount > 0)),
            Indicator("Stammdaten",
                building.Quality.CompletenessPercentage >= 90,
                building.Quality.CompletenessPercentage > 0),
            Indicator("Jahreswerte", meters.Any(x =>
                x.Readings.AnnualValueStatus ==
                AnnualValueStatus.CompleteYear),
                meters.Any()),
            Indicator("Anlagen", systems.Count > 0, false),
            new("Dokumente", "Yellow",
                "Canonical snapshots contain no document summary.")
        ];

    private static CompletenessIndicator Indicator(
        string name, bool complete, bool partial) =>
        new(name, complete ? "Green" : partial ? "Yellow" : "Red",
            complete ? "Complete" : partial ? "Partially available" :
                "Not available");

    private static string? Address(BuildingCanonicalSnapshot building)
    {
        var first = string.Join(" ", new[]
            { building.Street, building.HouseNumber }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        var second = string.Join(" ", new[]
            { building.PostalCode, building.City }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        var value = string.Join(", ", new[] { first, second }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static void Validate(ObjectSearchQuery query)
    {
        if (query.FromUtc >= query.ToUtc)
            throw new ArgumentException("FromUtc must be before ToUtc.");
        if (query.Page < 1)
            throw new ArgumentOutOfRangeException(nameof(query.Page));
        if (query.PageSize is < 1 or > 200)
            throw new ArgumentOutOfRangeException(nameof(query.PageSize));
    }

    private sealed record Reading(
        MeterCanonicalSnapshot Meter,
        CanonicalMeterReading Value);

    private sealed record ConvertedReading(
        MeterCanonicalSnapshot Meter,
        DateTime Timestamp,
        decimal Value);
}
