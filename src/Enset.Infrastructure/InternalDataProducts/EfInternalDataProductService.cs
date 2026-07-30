using Enset.Application.CanonicalSnapshots;
using Enset.Application.GoldProfiles;
using Enset.Application.InternalDataProducts;
using Enset.Domain.Curation;
using Enset.Domain.GoldProfiles;
using Enset.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Enset.Infrastructure.InternalDataProducts;

public sealed class EfInternalDataProductService(
    EnsetDbContext db,
    ICanonicalSnapshotReader snapshots,
    TimeProvider clock) :
    IBuildingSummaryProductService,
    IMeterSummaryProductService,
    ICustomerSummaryProductService,
    IPortfolioSummaryProductService,
    IImportQualityProductService
{
    async Task<BuildingSummaryProduct?>
        IBuildingSummaryProductService.GetAsync(
            Guid id,
            CancellationToken ct)
    {
        var building = await snapshots.GetBuilding(id, ct);
        if (building is null)
            return null;
        var portfolio = await snapshots.GetPortfolio(ct);
        var meters = portfolio.Meters
            .Where(x => x.BuildingId == id)
            .ToArray();
        var energy = Energy(meters);
        var totals = Totals(energy);
        return new(
            building.BuildingId,
            building.BuildingNumber,
            building.Name,
            building.CustomerId,
            building.CustomerNumber,
            building.CustomerName,
            building.BuildingType,
            building.UsageType,
            building.BuildingState,
            building.PostalCode,
            building.City,
            Address(building),
            building.GrossFloorArea,
            building.HeatedArea,
            building.ConstructionYear,
            building.RenovationYear,
            meters.Length,
            meters.Count(x => x.Direction?.ToString() == "Consumption"),
            meters.Count(x => x.Direction?.ToString() == "Production"),
            meters.Count(x => x.Direction?.ToString() == "Bidirectional"),
            totals.Consumption,
            totals.Generation,
            totals.Unit,
            energy.Count == 0
                ? "NotAvailable"
                : "CanonicalMeasurementProjection",
            Period(meters),
            energy,
            Maturity(building.Quality),
            0,
            Warnings(meters),
            Gold(building.Version),
            Suitability(building.Suitability.Benchmark),
            Suitability(building.Suitability.Benchmark),
            Suitability(building.Suitability.Navigator),
            Suitability(building.Suitability.Navigator));
    }

    async Task<MeterSummaryProduct?>
        IMeterSummaryProductService.GetAsync(
            Guid id,
            CancellationToken ct)
    {
        var meter = await snapshots.GetMeter(id, ct);
        if (meter is null)
            return null;
        var readings = meter.Readings;
        long? expected = readings.IntervalSeconds > 0 &&
                         readings.PeriodStart.HasValue &&
                         readings.PeriodEnd.HasValue
            ? (long)Math.Floor(
                (readings.PeriodEnd.Value -
                 readings.PeriodStart.Value).TotalSeconds /
                readings.IntervalSeconds.Value) + 1
            : null;
        long? missing = expected.HasValue
            ? Math.Max(0, expected.Value - readings.MeasurementCount)
            : null;
        return new(
            meter.MeterId,
            meter.MeterNumber,
            meter.Name,
            meter.BuildingId,
            meter.BuildingNumber,
            meter.BuildingName,
            meter.CustomerId,
            meter.CustomerName,
            meter.Medium?.ToString() ?? "Unknown",
            meter.Direction?.ToString() ?? "Unknown",
            meter.Unit?.ToString() ?? "Unknown",
            meter.UsageType,
            readings.AnnualValue,
            readings.AnnualValueStatus.ToString(),
            readings.PeriodStart.HasValue || readings.PeriodEnd.HasValue
                ? new(readings.PeriodStart, readings.PeriodEnd)
                : null,
            readings.MeasurementCount,
            readings.PeriodStart,
            readings.PeriodEnd,
            readings.IntervalSeconds / 60,
            expected,
            readings.MeasurementCount,
            missing,
            readings.InvalidCount,
            readings.EstimatedCount,
            readings.InterpolatedCount,
            readings.MeasuredCount,
            readings.DerivedCount,
            readings.CompletenessPercentage,
            Maturity(meter.Quality),
            0,
            Gold(meter.Version),
            Suitability(meter.Suitability.Navigator),
            Suitability(meter.Suitability.Navigator),
            Suitability(meter.Suitability.Benchmark),
            Suitability(meter.Suitability.Benchmark))
        {
            ReadingType = readings.ReadingType?.ToString(),
            Quantity = readings.Quantity?.ToString(),
            IntervalSeconds = readings.IntervalSeconds,
            AnnualValueStatus = readings.AnnualValueStatus.ToString()
        };
    }

    async Task<CustomerSummaryProduct?>
        ICustomerSummaryProductService.GetAsync(
            Guid id,
            CancellationToken ct)
    {
        var customer = await snapshots.GetCustomer(id, ct);
        if (customer is null)
            return null;
        var portfolio = await snapshots.GetPortfolio(ct);
        var buildings = portfolio.Buildings
            .Where(x => x.CustomerId == id)
            .ToArray();
        var buildingIds = buildings
            .Select(x => x.BuildingId)
            .ToHashSet();
        var meters = portfolio.Meters
            .Where(x =>
                x.BuildingId.HasValue &&
                buildingIds.Contains(x.BuildingId.Value))
            .ToArray();
        var energy = Energy(meters);
        var totals = Totals(energy);
        var quality = buildings
            .Select(x => x.Quality)
            .Concat(meters.Select(x => x.Quality))
            .Append(customer.Quality)
            .ToArray();
        var suitability = new Dictionary<string, ReadinessSummary>
        {
            ["Leb"] = Suitability(customer.Suitability.Leb),
            ["Navigator"] = Suitability(customer.Suitability.Navigator),
            ["Benchmark"] = Suitability(customer.Suitability.Benchmark),
            ["Iso50001"] = Suitability(customer.Suitability.Iso50001)
        };
        return new(
            customer.CustomerId,
            customer.CustomerNumber,
            customer.Name,
            customer.ContactPerson,
            customer.Email,
            customer.Phone,
            customer.PostalCode,
            customer.City,
            buildings.Length,
            meters.Length,
            portfolio.EnergySystems.Count(x =>
                x.BuildingId.HasValue &&
                buildingIds.Contains(x.BuildingId.Value)),
            totals.Consumption,
            totals.Generation,
            energy,
            Maturity(quality),
            buildings.Count(x =>
                x.Version.Status != GoldProfileReleaseStatus.Released),
            meters.Count(x =>
                x.Version.Status != GoldProfileReleaseStatus.Released),
            0,
            Warnings(meters),
            suitability);
    }

    async Task<PortfolioSummaryProduct>
        IPortfolioSummaryProductService.GetAsync(CancellationToken ct)
    {
        var portfolio = await snapshots.GetPortfolio(ct);
        var energy = Energy(portfolio.Meters);
        var totals = Totals(energy);
        var quality = portfolio.Customers.Select(x => x.Quality)
            .Concat(portfolio.Buildings.Select(x => x.Quality))
            .Concat(portfolio.Meters.Select(x => x.Quality))
            .Concat(portfolio.EnergySystems.Select(x => x.Quality))
            .ToArray();
        var releasedBuildings = portfolio.Buildings.Count(x =>
            x.Version.Status == GoldProfileReleaseStatus.Released);
        var releasedMeters = portfolio.Meters.Count(x =>
            x.Version.Status == GoldProfileReleaseStatus.Released);
        var now = clock.GetUtcNow().UtcDateTime;
        var reports = db.ImportReports.AsNoTracking();
        var successful =
            new[] { "Committed", "Completed", "Succeeded", "Successful" };
        var failed = new[] { "Failed", "Rejected" };
        var lastSuccess = await reports
            .Where(x => successful.Contains(x.Status))
            .MaxAsync(x => (DateTime?)x.UpdatedAt, ct);
        var suitability = new[]
        {
            PortfolioSuitability(
                "BuildingBenchmark",
                portfolio.Buildings.Select(x =>
                    x.Suitability.Benchmark)),
            PortfolioSuitability(
                "EnergyBenchmark",
                portfolio.Buildings.Select(x =>
                    x.Suitability.Benchmark)),
            PortfolioSuitability(
                "NormalizedLoadProfile",
                portfolio.Meters.Select(x =>
                    x.Suitability.Navigator)),
            PortfolioSuitability(
                "NormalizedGenerationProfile",
                portfolio.Meters.Select(x =>
                    x.Suitability.Navigator))
        };
        return new(
            portfolio.Customers.Count,
            portfolio.Customers.Count(x => x.IsActive),
            portfolio.Buildings.Count,
            portfolio.Buildings.Count(x => x.IsActive),
            portfolio.Meters.Count,
            portfolio.Meters.Count(x => x.IsActive),
            totals.Consumption,
            totals.Generation,
            energy,
            Maturity(quality),
            releasedBuildings,
            portfolio.Buildings.Count - releasedBuildings,
            releasedMeters,
            portfolio.Meters.Count - releasedMeters,
            portfolio.Meters.Count - releasedMeters,
            0,
            0,
            suitability[0].ReadyScopeCount,
            suitability[1].ReadyScopeCount,
            suitability[2].ReadyScopeCount,
            suitability[3].ReadyScopeCount,
            suitability.Sum(x => x.BlockedScopeCount),
            portfolio.Buildings.Count(building =>
                portfolio.Meters.All(meter =>
                    meter.BuildingId != building.BuildingId)),
            portfolio.Meters.Count(x =>
                x.Readings.MeasurementCount == 0),
            portfolio.Meters.Count(x =>
                x.Readings.AnnualValueStatus ==
                AnnualValueStatus.IncompleteYear),
            portfolio.Buildings.Count - releasedBuildings,
            lastSuccess,
            await reports.CountAsync(x =>
                failed.Contains(x.Status) &&
                x.UpdatedAt >= now.AddDays(-30), ct),
            await db.ImportIssues.CountAsync(x => !x.IsResolved, ct),
            Warnings(portfolio.Meters),
            suitability,
            now);
    }

    async Task<ImportQualityProduct>
        IImportQualityProductService.GetAsync(CancellationToken ct)
    {
        var reports = db.ImportReports.AsNoTracking();
        var rows = await reports
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .Select(x => new
            {
                x.ImportId,
                x.SourceFileName,
                x.SourceFileContentType,
                x.CreatedAt,
                x.Status,
                Issues = x.Issues.Count,
                Blocking = x.Issues.Count(i =>
                    !i.IsResolved &&
                    (i.Severity == "Error" ||
                     i.Severity == "Critical")),
                Warnings = x.Issues.Count(i =>
                    !i.IsResolved &&
                    i.Severity == "Warning"),
                Committed = x.AuditTrail
                    .Where(a => a.Action == "Commit")
                    .Max(a => (DateTime?)a.Timestamp)
            })
            .ToListAsync(ct);
        var issues = db.ImportIssues
            .AsNoTracking()
            .Where(x => !x.IsResolved);
        var successful =
            new[] { "Committed", "Completed", "Succeeded", "Successful" };
        var failed = new[] { "Failed", "Rejected" };
        var total = await reports.CountAsync(ct);
        var successfulCount = await reports.CountAsync(
            x => successful.Contains(x.Status), ct);
        var failedCount = await reports.CountAsync(
            x => failed.Contains(x.Status), ct);
        var versions = await db.GoldProfileVersions
            .AsNoTracking()
            .Where(x => x.IsCurrent)
            .GroupBy(x => x.ReleaseStatus)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToListAsync(ct);
        return new(
            total,
            successfulCount,
            failedCount,
            total - successfulCount - failedCount,
            await reports.CountAsync(
                x => x.Issues.Any(i => !i.IsResolved), ct),
            rows.Select(x => new ImportQualityItem(
                x.ImportId,
                x.SourceFileName,
                x.SourceFileContentType,
                x.CreatedAt,
                x.Status,
                x.Issues,
                x.Blocking,
                x.Warnings,
                null,
                null,
                x.Committed)).ToArray(),
            await issues.CountAsync(ct),
            await issues.CountAsync(
                x => x.Severity == "Error" ||
                     x.Severity == "Critical", ct),
            await issues.CountAsync(
                x => x.Severity == "Warning", ct),
            await issues.CountAsync(
                x => x.RequiresUserDecision, ct),
            await db.CurationTasks.CountAsync(
                x => x.Status == CurationTaskStatus.Open, ct),
            0,
            0,
            versions
                .Where(x =>
                    x.Key == GoldProfileReleaseStatus.Released)
                .Sum(x => x.Count),
            versions
                .Where(x =>
                    x.Key == GoldProfileReleaseStatus.Draft)
                .Sum(x => x.Count),
            clock.GetUtcNow().UtcDateTime);
    }

    private static DataMaturitySummary Maturity(SnapshotQuality quality) =>
        quality.Level switch
        {
            DataMaturityLevel.Gold =>
                new(0, 0, 1, 100),
            DataMaturityLevel.Silver =>
                new(0, 1, 0, 0),
            _ => new(1, 0, 0, 0)
        };

    private static DataMaturitySummary Maturity(
        IReadOnlyCollection<SnapshotQuality> values)
    {
        if (values.Count == 0)
            return new(0, 0, 0, 0);
        var bronze = values.Count(x =>
            x.Level == DataMaturityLevel.Bronze);
        var silver = values.Count(x =>
            x.Level == DataMaturityLevel.Silver);
        var gold = values.Count(x =>
            x.Level == DataMaturityLevel.Gold);
        return new(
            bronze,
            silver,
            gold,
            decimal.Round(gold * 100m / values.Count, 2));
    }

    private static GoldProfileSummary Gold(CanonicalVersion version) =>
        new(
            version.SnapshotId,
            version.Version,
            version.Status.ToString(),
            null);

    private static ReadinessSummary Suitability(
        SuitabilityStatus status) =>
        status == SuitabilityStatus.Suitable
            ? new(DataProductReadinessStatus.Ready, 100, [])
            : new(
                DataProductReadinessStatus.NotReady,
                0,
                ["NotSuitable"]);

    private static PortfolioReadinessSummary PortfolioSuitability(
        string product,
        IEnumerable<SuitabilityStatus> values)
    {
        var all = values.ToArray();
        var suitable = all.Count(x =>
            x == SuitabilityStatus.Suitable);
        var blocked = all.Length - suitable;
        var percentage = all.Length == 0
            ? (int?)null
            : suitable * 100 / all.Length;
        return new(
            product,
            suitable == all.Length && all.Length > 0
                ? DataProductReadinessStatus.Ready
                : suitable > 0
                    ? DataProductReadinessStatus.ReadyWithWarnings
                    : DataProductReadinessStatus.NotReady,
            percentage,
            suitable,
            blocked,
            blocked == 0 ? [] : ["NotSuitable"],
            blocked == 0
                ? []
                : ["Canonical Snapshot vervollständigen"],
            product.Contains("Building", StringComparison.Ordinal)
                ? "/buildings"
                : "/meters");
    }

    private static List<EnergySummaryItem> Energy(
        IEnumerable<MeterCanonicalSnapshot> meters) =>
        meters
            .Where(x =>
                x.Readings.AnnualValueStatus ==
                    AnnualValueStatus.CompleteYear &&
                x.Readings.AnnualValue.HasValue &&
                x.Unit.HasValue)
            .GroupBy(x => new
            {
                Carrier = x.Medium?.ToString() ?? "Unknown",
                Direction = x.Direction?.ToString() ?? "Unknown",
                Unit = x.Unit!.Value.ToString()
            })
            .Select(group => new EnergySummaryItem(
                group.Key.Carrier,
                group.Key.Direction,
                group.Sum(x => x.Readings.AnnualValue!.Value),
                group.Key.Unit,
                "CanonicalMeasurementProjection",
                Period(group),
                group.Count()))
            .OrderBy(x => x.EnergyCarrier)
            .ThenBy(x => x.Unit)
            .ToList();

    private static ReferencePeriod? Period(
        IEnumerable<MeterCanonicalSnapshot> meters)
    {
        var values = meters.ToArray();
        var starts = values
            .Select(x => x.Readings.PeriodStart)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToArray();
        var ends = values
            .Select(x => x.Readings.PeriodEnd)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToArray();
        return starts.Length == 0 && ends.Length == 0
            ? null
            : new(
                starts.Length == 0 ? null : starts.Min(),
                ends.Length == 0 ? null : ends.Max());
    }

    private static (
        decimal? Consumption,
        decimal? Generation,
        string? Unit) Totals(
            IReadOnlyList<EnergySummaryItem> energy)
    {
        var units = energy
            .Select(x => x.Unit)
            .Distinct()
            .ToArray();
        if (units.Length != 1)
            return (null, null, null);
        decimal? Sum(params string[] directions)
        {
            var rows = energy
                .Where(x => directions.Contains(x.Direction))
                .ToArray();
            return rows.Length == 0
                ? null
                : rows.Sum(x => x.AnnualValue);
        }
        return (
            Sum("Consumption"),
            Sum("Production"),
            units[0]);
    }

    private static IReadOnlyList<DataQualityWarning> Warnings(
        IEnumerable<MeterCanonicalSnapshot> meters)
    {
        var values = meters.ToArray();
        var result = new List<DataQualityWarning>();
        if (values.Length == 0)
            result.Add(new(
                "NO_METERS",
                "Warning",
                "Keine Zählpunkte vorhanden."));
        if (values.Any(x =>
                x.Readings.AnnualValueStatus ==
                AnnualValueStatus.IncompleteYear))
            result.Add(new(
                "INCOMPLETE_YEAR",
                "Information",
                "Mindestens eine Messreihe enthält kein vollständiges Jahr."));
        if (values.Select(x => x.Unit).Distinct().Count() > 1)
            result.Add(new(
                "INCOMPATIBLE_UNITS",
                "Warning",
                "Gesamtwerte sind wegen unterschiedlicher Einheiten nicht verfügbar."));
        return result;
    }

    private static string? Address(
        BuildingCanonicalSnapshot building)
    {
        var line = string.Join(
            " ",
            new[] { building.Street, building.HouseNumber }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(line) ? null : line;
    }
}
