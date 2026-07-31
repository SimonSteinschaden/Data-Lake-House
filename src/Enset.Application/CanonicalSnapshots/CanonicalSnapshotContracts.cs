using Enset.Domain.Curation;
using Enset.Domain.Data;
using Enset.Domain.Energy;
using Enset.Domain.GoldProfiles;
using Enset.Domain.Quality;
using Enset.Application.Quality;

namespace Enset.Application.CanonicalSnapshots;

public enum AnnualValueStatus
{
    NotAvailable,
    IncompleteYear,
    CompleteYear
}

public enum SuitabilityStatus
{
    NotSuitable,
    Suitable
}

public sealed record SnapshotSuitability(
    SuitabilityStatus Leb,
    SuitabilityStatus Navigator,
    SuitabilityStatus Benchmark,
    SuitabilityStatus Iso50001);

public sealed record SnapshotQuality(
    DataMaturityLevel Level,
    int CompletenessPercentage,
    int ValidityPercentage,
    int ConsistencyPercentage,
    int CurationPercentage);

public sealed record CanonicalVersion(
    Guid SnapshotId,
    int Version,
    DateTime CreatedAt,
    string Source,
    GoldProfileReleaseStatus Status);

public sealed record CanonicalReadingSummary(
    long MeasurementCount,
    DateTime? PeriodStart,
    DateTime? PeriodEnd,
    MeterUnit? Unit,
    MeterReadingType? ReadingType,
    MeterQuantity? Quantity,
    int? IntervalSeconds,
    long InvalidCount,
    long EstimatedCount,
    long InterpolatedCount,
    long MeasuredCount,
    long DerivedCount,
    decimal? CompletenessPercentage,
    decimal? AnnualValue,
    AnnualValueStatus AnnualValueStatus)
{
    public int? AnnualValueReferenceYear { get; init; }
}

public sealed record CanonicalMeterReading(
    DateTime Timestamp,
    decimal Value,
    MeterUnit? Unit,
    MeterReadingType ReadingType,
    int? IntervalSeconds,
    string QualityStatus,
    string Source,
    bool IsCalculated);

public sealed record CustomerCanonicalSnapshot(
    Guid CustomerId,
    string CustomerNumber,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? PostalCode,
    string? City,
    string? MunicipalityId,
    string? MunicipalityName,
    bool IsActive,
    SnapshotQuality Quality,
    SnapshotSuitability Suitability,
    CanonicalVersion Version);

public sealed record BuildingCanonicalSnapshot(
    Guid BuildingId,
    string BuildingNumber,
    string Name,
    Guid? CustomerId,
    string? CustomerNumber,
    string? CustomerName,
    string? BuildingType,
    string? UsageType,
    string? BuildingState,
    string? Street,
    string? HouseNumber,
    string? PostalCode,
    string? City,
    string? MunicipalityId,
    string? MunicipalityName,
    int? ConstructionYear,
    int? RenovationYear,
    decimal? GrossFloorArea,
    decimal? NetFloorArea,
    decimal? ConditionedFloorArea,
    decimal? HeatedArea,
    decimal? CooledArea,
    decimal? BuildingVolume,
    int? NumberOfFloors,
    bool IsActive,
    SnapshotQuality Quality,
    SnapshotSuitability Suitability,
    CanonicalVersion Version)
{
    public string? MunicipalityNumber { get; init; }
    public string? MainRegion { get; init; }
    public BuildingGoldAssessment GoldAssessment { get; init; } =
        BuildingGoldDefinition.Evaluate(null, null, null, null);
    public OperationalBuildingQualityAssessment? QualityAssessment { get; init; }
    public DataMaturityLevel BuildingCoreQualityLevel =>
        QualityAssessment?.Assessment.BuildingCoreQuality ?? GoldAssessment.MaturityLevel;
    public DataMaturityLevel OverallQualityLevel =>
        QualityAssessment?.Assessment.OverallQualityLevel ?? GoldAssessment.MaturityLevel;
    public int GoldProgressPercentage => QualityAssessment?.Assessment.GoldProgress.Percentage ?? 0;
    public int BronzeCount => QualityAssessment?.Assessment.GoldProgress.BronzeCount ?? 0;
    public int SilverCount => QualityAssessment?.Assessment.GoldProgress.SilverCount ?? 0;
    public int GoldCount => QualityAssessment?.Assessment.GoldProgress.GoldCount ?? 0;
    public string InventoryDeclarationStatus => QualityAssessment?.InventoryDeclarationStatus ?? "Nicht bestätigt";
    public AnnualEnergyStatus AnnualConsumptionStatus =>
        QualityAssessment?.Assessment.AnnualConsumptionStatus ?? AnnualEnergyStatus.NotAvailable;
    public AnnualEnergyStatus AnnualProductionStatus =>
        QualityAssessment?.Assessment.AnnualProductionStatus ?? AnnualEnergyStatus.NotAvailable;
    public IReadOnlyList<ScopeQuality> MeterQualitySummaries =>
        QualityAssessment?.Assessment.MeterQualities ?? [];
    public IReadOnlyList<ScopeQuality> EnergySystemQualitySummaries =>
        QualityAssessment?.Assessment.EnergySystemQualities ?? [];
    public IReadOnlyList<string> BlockingReasons => QualityAssessment?.Assessment.BlockingReasons ?? [];
    public IReadOnlyList<string> Warnings => QualityAssessment?.Assessment.Warnings ?? [];
    public IReadOnlyList<string> NextActions => QualityAssessment?.NextActions ?? [];
}

public sealed record MeterCanonicalSnapshot(
    Guid MeterId,
    string MeterNumber,
    string Name,
    Guid? BuildingId,
    string? BuildingNumber,
    string? BuildingName,
    Guid? CustomerId,
    string? CustomerName,
    MeterMedium? Medium,
    MeterDirection? Direction,
    MeterQuantity? Quantity,
    MeterUnit? Unit,
    string? UsageType,
    bool IsActive,
    CanonicalReadingSummary Readings,
    SnapshotQuality Quality,
    SnapshotSuitability Suitability,
    CanonicalVersion Version)
{
    public string? CustomerNumber { get; init; }
    public string? ExternalIdentifier { get; init; }
    public string? MeterType { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public IReadOnlyList<CanonicalMeterReading> ReadingValues { get; init; } = [];
    public MeterQualityAssessment? QualityAssessment { get; init; }
    public DataMaturityLevel QualityLevel =>
        QualityAssessment?.QualityLevel ?? Quality.Level;
    public ProfileAnalysisStatus ProfileAnalysisStatus =>
        QualityAssessment?.ProfileAnalysisStatus ?? Enset.Domain.Quality.ProfileAnalysisStatus.NotAnalyzed;
    public Guid? CurrentAnalysisId => QualityAssessment?.CurrentAnalysisId;
    public string? AnalysisVersion => QualityAssessment?.AnalysisVersion;
    public decimal? CompletenessPercentage => QualityAssessment?.CompletenessPercentage;
    public int OpenIssueCount => QualityAssessment?.OpenIssueCount ?? 0;
    public int BlockingIssueCount => QualityAssessment?.BlockingIssueCount ?? 0;
    public int OpenAnomalyCount => QualityAssessment?.OpenAnomalyCount ?? 0;
    public DateTime? LastAnalyzedAtUtc => QualityAssessment?.LastAnalyzedAtUtc;
    public string ConfirmationStatus => QualityAssessment?.ConfirmationStatus ?? "Nicht bestätigt";
    public IReadOnlyList<string> BlockingReasons => QualityAssessment?.BlockingReasons ?? [];
    public IReadOnlyList<string> Warnings => QualityAssessment?.Warnings ?? [];
    public IReadOnlyList<string> NextActions => QualityAssessment?.NextActions ?? [];
}

public sealed record EnergySystemCanonicalSnapshot(
    Guid EnergySystemId,
    string? Type,
    string? EnergyCarrier,
    string? Purpose,
    decimal? InstalledPower,
    int? ConstructionYear,
    Guid? BuildingId,
    SnapshotQuality Quality,
    SnapshotSuitability Suitability,
    CanonicalVersion Version)
{
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public EnergySystemQualityAssessment? QualityAssessment { get; init; }
    public DataMaturityLevel QualityLevel =>
        QualityAssessment?.QualityLevel ?? Quality.Level;
    public string ConfirmationStatus => QualityAssessment?.ConfirmationStatus ?? "Nicht bestätigt";
    public IReadOnlyList<string> MissingRequirements => QualityAssessment?.MissingRequirements ?? [];
    public IReadOnlyList<string> BlockingReasons => QualityAssessment?.BlockingReasons ?? [];
    public IReadOnlyList<string> Warnings => QualityAssessment?.Warnings ?? [];
    public IReadOnlyList<string> NextActions => QualityAssessment?.NextActions ?? [];
}

public sealed record CanonicalSnapshotSet(
    IReadOnlyList<CustomerCanonicalSnapshot> Customers,
    IReadOnlyList<BuildingCanonicalSnapshot> Buildings,
    IReadOnlyList<MeterCanonicalSnapshot> Meters,
    IReadOnlyList<EnergySystemCanonicalSnapshot> EnergySystems);

public interface ICanonicalSnapshotReader
{
    Task<IReadOnlyList<CustomerCanonicalSnapshot>> GetCustomers(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<BuildingCanonicalSnapshot>> GetBuildings(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<MeterCanonicalSnapshot>> GetMeters(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
    Task<CustomerCanonicalSnapshot?> GetCustomer(
        Guid id, CancellationToken cancellationToken);
    Task<BuildingCanonicalSnapshot?> GetBuilding(
        Guid id, CancellationToken cancellationToken);
    Task<MeterCanonicalSnapshot?> GetMeter(
        Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<EnergySystemCanonicalSnapshot>> GetEnergySystems(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);
    Task<EnergySystemCanonicalSnapshot?> GetEnergySystem(
        Guid id, CancellationToken cancellationToken);
    Task<CanonicalSnapshotSet> GetPortfolio(
        CancellationToken cancellationToken);
}
