namespace Enset.Application.Analytics;

public sealed record AnalyticsQuery(
    int Year,
    Guid? CustomerId = null,
    Guid? BuildingId = null,
    Guid? MeterId = null,
    string? Region = null,
    string? PostalCode = null,
    string Aggregation = "Hour");

public sealed record PortfolioSummaryDataProduct(
    int CustomerCount,
    int BuildingCount,
    int MeterCount,
    int DocumentCount,
    int EnergySystemCount,
    DateTime CalculatedAt);

public sealed record RegionalBuildingDistributionItem(
    string? State,
    string? District,
    string? Municipality,
    string? PostalCode,
    int BuildingCount,
    int CustomerCount,
    int MeterCount);

public sealed record RegionalBuildingDistributionDataProduct(
    IReadOnlyList<RegionalBuildingDistributionItem> Regions,
    DateTime CalculatedAt);

public sealed record TimeSeriesPoint(DateTime Timestamp, decimal Value);

public sealed record ElectricityPortfolioLoadProfileDataProduct(
    int Year,
    string Aggregation,
    string Quantity,
    string? Unit,
    IReadOnlyList<TimeSeriesPoint> Points,
    decimal DataCoveragePercent,
    DateTime? LastReadingAt,
    DateTime CalculatedAt);

public sealed record MonthlyConsumptionPoint(
    int Month,
    decimal? Value,
    decimal? PreviousYearValue);

public sealed record MonthlyElectricityConsumptionDataProduct(
    int Year,
    string Quantity,
    string? Unit,
    IReadOnlyList<MonthlyConsumptionPoint> Months,
    decimal? TotalConsumption,
    decimal DataCoveragePercent,
    DateTime CalculatedAt);

public sealed record MeteringCoverageSummaryDataProduct(
    int BuildingsWithReadings,
    int BuildingsWithoutReadings,
    int ActiveMeters,
    int InactiveMeters,
    int MetersWithoutReadings,
    DateTime? LastReadingAt,
    decimal CoveragePercent,
    DateTime CalculatedAt);

public sealed record DataQualitySummaryDataProduct(
    decimal? OverallScore,
    int MissingAssignments,
    int StaleReadings,
    int IncompleteMasterData,
    int InvalidUnits,
    int? TimeSeriesGaps,
    int? Duplicates,
    int CriticalWarnings,
    DateTime CalculatedAt);

public sealed record NamedCount(string Name, int Count);

public sealed record EnergyPortfolioStructureDataProduct(
    int BuildingsWithElectricityMeters,
    int BuildingsWithHeatMeters,
    int BuildingsWithGasMeters,
    int BuildingsWithWaterMeters,
    int EnergySystemCount,
    IReadOnlyList<NamedCount> EnergySystemsByType,
    IReadOnlyList<NamedCount> MetersByCarrier,
    DateTime CalculatedAt);

public sealed record ManagementWarning(
    string Code,
    string Severity,
    string Message,
    int AffectedCount);

public sealed record ManagementWarningsDataProduct(
    IReadOnlyList<ManagementWarning> Warnings,
    int TotalAffectedCount,
    DateTime CalculatedAt);

public sealed record ManagementWarningDetail(
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

public sealed record ManagementWarningDetailsDataProduct(
    IReadOnlyList<ManagementWarningDetail> Warnings,
    int TotalCount,
    int Page,
    int PageSize,
    DateTime CalculatedAt);

public sealed record ConsumptionByLocationItem(
    Guid BuildingId,
    string Building,
    string? Municipality,
    string? PostalCode,
    decimal Consumption,
    string Unit,
    int MeterCount,
    decimal? DataCoveragePercent);

public sealed record EnergyConsumptionByLocationDataProduct(
    int Year,
    IReadOnlyList<ConsumptionByLocationItem> Locations,
    DateTime CalculatedAt);

public sealed record ConsumptionCategoryItem(
    string Name,
    decimal Consumption,
    decimal SharePercent,
    string Unit,
    int MeterCount,
    int BuildingCount);

public sealed record EnergyConsumptionByUsageTypeDataProduct(
    int Year,
    IReadOnlyList<ConsumptionCategoryItem> UsageTypes,
    DateTime CalculatedAt);

public sealed record TopEnergySystemItem(
    Guid EnergySystemId,
    string EnergySystem,
    string? Building,
    string SystemType,
    string? UsageType,
    decimal Consumption,
    decimal SharePercent,
    string Unit,
    decimal? DataCoveragePercent);

public sealed record TopEnergySystemsByConsumptionDataProduct(
    int Year,
    IReadOnlyList<TopEnergySystemItem> EnergySystems,
    DateTime CalculatedAt);

public sealed record ConsumptionByCarrierItem(
    string Carrier,
    decimal Consumption,
    decimal SharePercent,
    string Unit,
    int BuildingCount,
    int MeterCount);

public sealed record EnergyConsumptionByCarrierDataProduct(
    int Year,
    string Unit,
    IReadOnlyList<ConsumptionByCarrierItem> Carriers,
    DateTime CalculatedAt);
