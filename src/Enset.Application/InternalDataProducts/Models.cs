using Enset.Application.GoldProfiles;
using Enset.Application.CanonicalSnapshots;

namespace Enset.Application.InternalDataProducts;

public sealed record ReferencePeriod(DateTime? From, DateTime? To);
public sealed record DataMaturitySummary(
    int BronzeMaturity, int SilverMaturity, int GoldMaturity,
    [property: Obsolete(
        "Diese Portfolio-Verteilungskennzahl ist keine Gold-Vollständigkeit. " +
        "Für Gebäude ist GoldAssessment zu verwenden.")]
    decimal GoldMaturityPercentage);
public sealed record ReadinessSummary(
    DataProductReadinessStatus Status, int Percentage,
    IReadOnlyList<string> BlockingIssues);
public sealed record GoldProfileSummary(
    Guid? VersionId, int? VersionNumber, string ReleaseStatus, string? SnapshotHash);
public sealed record DataQualityWarning(string Code, string Severity, string Message);
public sealed record EnergySummaryItem(
    string EnergyCarrier, string Direction, decimal AnnualValue, string Unit,
    string Origin, ReferencePeriod? ReferencePeriod, int MeterCount = 1);
public sealed record PortfolioReadinessSummary(
    string Product, DataProductReadinessStatus Status, int? Percentage,
    int ReadyScopeCount, int BlockedScopeCount,
    IReadOnlyList<string> TopBlockers,
    IReadOnlyList<string> Recommendations, string DetailPath);

public sealed record BuildingSummaryProduct(
    Guid BuildingId, string BuildingNumber, string Name,
    Guid? CustomerId, string? CustomerNumber, string? CustomerName,
    string? BuildingType, string? UsageType, string? BuildingState,
    string? PostalCode, string? City, string? Address,
    decimal? GrossFloorArea, decimal? HeatedArea,
    int? ConstructionYear, int? RenovationYear,
    int MeterCount, int ConsumptionMeterCount, int ProductionMeterCount,
    int BidirectionalMeterCount, decimal? AnnualConsumption,
    decimal? AnnualGeneration, string? Unit, string ValueOrigin,
    ReferencePeriod? ReferencePeriod, IReadOnlyList<EnergySummaryItem> EnergySummaries,
    DataMaturitySummary Maturity, BuildingGoldAssessment GoldAssessment,
    int OpenCurationTaskCount,
    IReadOnlyList<DataQualityWarning> DataQualityWarnings,
    GoldProfileSummary GoldProfile,
    ReadinessSummary BuildingBenchmarkReadiness,
    ReadinessSummary EnergyBenchmarkReadiness,
    ReadinessSummary NormalizedLoadProfileReadiness,
    ReadinessSummary NormalizedGenerationProfileReadiness)
{
    public string QualityLevel => GoldAssessment.MaturityLevel.ToString();
    public IReadOnlyDictionary<string, ReadinessSummary> Suitability =>
        new Dictionary<string, ReadinessSummary>
        {
            ["benchmark"] = BuildingBenchmarkReadiness,
            ["energyBenchmark"] = EnergyBenchmarkReadiness,
            ["navigatorLoad"] = NormalizedLoadProfileReadiness,
            ["navigatorGeneration"] = NormalizedGenerationProfileReadiness
        };

}

public sealed record MeterSummaryProduct(
    Guid MeteringPointId, string MeteringPointNumber, string InternalName,
    Guid? BuildingId, string? BuildingNumber, string? BuildingName,
    Guid? CustomerId, string? CustomerName,
    string Medium, string Direction, string Unit, string? UsageType,
    decimal? AnnualValue, string AnnualValueOrigin, ReferencePeriod? ReferencePeriod,
    long ReadingCount, DateTime? MeasurementStart, DateTime? MeasurementEnd,
    int? DetectedIntervalMinutes, long? ExpectedValueCount, long ActualValueCount,
    long? MissingValueCount, long InvalidValueCount, long EstimatedValueCount,
    long InterpolatedValueCount, long MeasuredValueCount, long DerivedValueCount,
    decimal? CompletenessPercentage, DataMaturitySummary Maturity,
    int OpenCurationTaskCount, GoldProfileSummary GoldProfile,
    ReadinessSummary NormalizedLoadProfileReadiness,
    ReadinessSummary NormalizedGenerationProfileReadiness,
    ReadinessSummary EegMatchingReadiness, ReadinessSummary PeerToPeerReadiness)
{
    public string? ReadingType { get; init; }
    public string? Quantity { get; init; }
    public int? IntervalSeconds { get; init; }
    public string AnnualValueStatus { get; init; } = "NotAvailable";
    public string QualityLevel =>
        Maturity.GoldMaturity > 0 ? "Gold" :
        Maturity.SilverMaturity > 0 ? "Silver" : "Bronze";
    public IReadOnlyDictionary<string, ReadinessSummary> Suitability =>
        new Dictionary<string, ReadinessSummary>
        {
            ["navigatorLoad"] = NormalizedLoadProfileReadiness,
            ["navigatorGeneration"] = NormalizedGenerationProfileReadiness,
            ["eegMatching"] = EegMatchingReadiness,
            ["peerToPeer"] = PeerToPeerReadiness
        };
}

public sealed record CustomerSummaryProduct(
    Guid CustomerId, string CustomerNumber, string OrganizationName,
    string? ContactPerson, string? Email, string? Phone,
    string? PostalCode, string? City, int BuildingCount, int MeterCount,
    int? EnergySystemCount, decimal? TotalAnnualConsumption,
    decimal? TotalAnnualGeneration, IReadOnlyList<EnergySummaryItem> EnergySummaries,
    DataMaturitySummary AverageMaturity, int BuildingsWithoutGoldProfile,
    int MetersWithoutGoldProfile, int OpenCurationTaskCount,
    IReadOnlyList<DataQualityWarning> DataQualityWarnings,
    IReadOnlyDictionary<string, ReadinessSummary> DataProductReadinessSummary)
{
    public string QualityLevel =>
        AverageMaturity.GoldMaturity > 0 ? "Gold" :
        AverageMaturity.SilverMaturity > 0 ? "Silver" : "Bronze";
    public IReadOnlyDictionary<string, ReadinessSummary> Suitability =>
        DataProductReadinessSummary;
}

public sealed record PortfolioSummaryProduct(
    int CustomerCount, int ActiveCustomerCount, int BuildingCount,
    int ActiveBuildingCount, int MeterCount, int ActiveMeterCount,
    decimal? TotalAnnualConsumption, decimal? TotalAnnualGeneration,
    IReadOnlyList<EnergySummaryItem> EnergySummaries,
    DataMaturitySummary AverageMaturity,
    int ReleasedBuildingGoldProfileCount, int DraftBuildingGoldProfileCount,
    int ReleasedMeterGoldProfileCount, int DraftMeterGoldProfileCount,
    int MetersWithoutReleasedGoldProfile,
    int OpenCurationTaskCount, int HighPriorityCurationTaskCount,
    int ReadyBuildingBenchmarkCount, int ReadyEnergyBenchmarkCount,
    int ReadyNormalizedLoadProfileCount, int ReadyNormalizedGenerationProfileCount,
    int BlockedReadinessCount, int BuildingsWithoutMeters, int MetersWithoutReadings,
    int MetersWithIncompleteProfiles, int BuildingsWithoutReleasedGoldProfile,
    DateTime? LastSuccessfulImportAt, int FailedImportCountLast30Days,
    int OpenImportIssueCount, IReadOnlyList<DataQualityWarning> DataQualityWarnings,
    IReadOnlyList<PortfolioReadinessSummary> ReadinessSummaries,
    DateTime CalculatedAt)
{
    public IReadOnlyDictionary<string, int> QualityLevelDistribution =>
        new Dictionary<string, int>
        {
            ["bronze"] = AverageMaturity.BronzeMaturity,
            ["silver"] = AverageMaturity.SilverMaturity,
            ["gold"] = AverageMaturity.GoldMaturity
        };
}

public sealed record ImportQualityItem(
    Guid ImportId, string? FileName, string? ImportType, DateTime CreatedAt,
    string Status, int IssueCount, int BlockingIssueCount, int WarningCount,
    Guid? TargetMeteringPointId, string? TargetMeteringPointNumber, DateTime? CommittedAt);
public sealed record ImportQualityProduct(
    int TotalImportCount, int SuccessfulImportCount, int FailedImportCount,
    int PendingImportCount, int ImportsWithOpenIssuesCount,
    IReadOnlyList<ImportQualityItem> LatestImports, int TotalOpenIssues,
    int TotalBlockingIssues, int TotalWarnings, int OpenResolutionCount,
    int OpenCurationTaskCount, int BuildingsBlockedByMissingData,
    int MetersBlockedByMissingData, int ReleasedGoldProfiles, int DraftGoldProfiles,
    DateTime CalculatedAt);

public interface IBuildingSummaryProductService
{
    Task<BuildingSummaryProduct?> GetAsync(Guid buildingId, CancellationToken cancellationToken);
}
public interface IMeterSummaryProductService
{
    Task<MeterSummaryProduct?> GetAsync(Guid meteringPointId, CancellationToken cancellationToken);
}
public interface ICustomerSummaryProductService
{
    Task<CustomerSummaryProduct?> GetAsync(Guid customerId, CancellationToken cancellationToken);
}
public interface IPortfolioSummaryProductService
{
    Task<PortfolioSummaryProduct> GetAsync(CancellationToken cancellationToken);
}
public interface IImportQualityProductService
{
    Task<ImportQualityProduct> GetAsync(CancellationToken cancellationToken);
}
