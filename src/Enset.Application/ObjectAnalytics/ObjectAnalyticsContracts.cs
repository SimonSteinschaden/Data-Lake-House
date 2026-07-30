using Enset.Application.CanonicalSnapshots;

namespace Enset.Application.ObjectAnalytics;

public sealed record ObjectSearchQuery(
    string? Search,
    string? EnergyCarrier,
    string? BuildingType,
    string? QualityLevel,
    Guid? CustomerId,
    DateTime FromUtc,
    DateTime ToUtc,
    int Page = 1,
    int PageSize = 25);

public sealed record ObjectSearchItem(
    Guid BuildingId,
    string BuildingNumber,
    string Name,
    Guid? CustomerId,
    string? Customer,
    string? BuildingType,
    string? Municipality,
    string? Address,
    IReadOnlyList<string> MeterNumbers,
    SnapshotQuality Quality,
    SnapshotSuitability Suitability);

public sealed record ObjectSearchResult(
    IReadOnlyList<ObjectSearchItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AnalyticsValue(
    decimal? Value,
    string Unit,
    string Status,
    string Source,
    string Rule,
    SnapshotQuality Quality,
    SnapshotSuitability Suitability);

public sealed record AnalyticsSeriesPoint(
    DateTime Timestamp,
    decimal Value);

public sealed record AnalyticsCategory(
    string Name,
    decimal Value,
    string Unit);

public sealed record AnalyticsWarning(
    string Code,
    string Severity,
    string Message,
    string Rule);

public sealed record CompletenessIndicator(
    string Name,
    string Status,
    string Reason);

public sealed record EnergySystemAnalyticsItem(
    Guid EnergySystemId,
    string? Type,
    string? EnergyCarrier,
    decimal? InstalledPower,
    int? ConstructionYear,
    string Status);

public sealed record BenchmarkResult(
    string Status,
    decimal? ObjectValue,
    decimal? ComparisonValue,
    string Unit,
    int ComparisonCount,
    string Rule);

public sealed record PreviousYearComparison(
    decimal? Current,
    decimal? Previous,
    decimal? AbsoluteChange,
    decimal? PercentChange,
    string Unit,
    string Status);

public sealed record ObjectAnalyticsProduct(
    Guid BuildingId,
    string BuildingNumber,
    string BuildingName,
    DateTime FromUtc,
    DateTime ToUtc,
    SnapshotQuality Quality,
    SnapshotSuitability Suitability,
    AnalyticsValue TotalConsumption,
    AnalyticsValue ElectricityConsumption,
    AnalyticsValue HeatConsumption,
    AnalyticsValue EnergyProduction,
    AnalyticsValue EnergyCosts,
    AnalyticsValue Co2Emissions,
    AnalyticsValue PeakLoad,
    DateTime? PeakLoadAt,
    AnalyticsValue ConsumptionPerSquareMeter,
    AnalyticsValue ConsumptionPerUser,
    AnalyticsValue SelfConsumption,
    AnalyticsValue SelfSufficiency,
    IReadOnlyList<AnalyticsSeriesPoint> MonthlyConsumption,
    IReadOnlyList<AnalyticsSeriesPoint> DailyLoadProfile,
    IReadOnlyList<AnalyticsSeriesPoint> WeeklyLoadProfile,
    IReadOnlyList<AnalyticsCategory> ConsumptionByCarrier,
    IReadOnlyList<AnalyticsCategory> ConsumptionByUsageType,
    IReadOnlyList<EnergySystemAnalyticsItem> EnergySystems,
    decimal? TotalInstalledPower,
    IReadOnlyList<AnalyticsWarning> Anomalies,
    IReadOnlyList<AnalyticsWarning> Warnings,
    IReadOnlyList<CompletenessIndicator> Completeness,
    PreviousYearComparison ConsumptionPreviousYear,
    BenchmarkResult Benchmark,
    DateTime CalculatedAtUtc);

public interface IObjectAnalyticsService
{
    Task<ObjectSearchResult> Search(
        ObjectSearchQuery query,
        CancellationToken cancellationToken);

    Task<ObjectAnalyticsProduct?> Analyze(
        Guid buildingId,
        ObjectSearchQuery query,
        CancellationToken cancellationToken);
}
