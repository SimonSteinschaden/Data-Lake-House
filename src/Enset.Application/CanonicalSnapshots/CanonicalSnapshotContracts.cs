using Enset.Domain.Curation;
using Enset.Domain.Data;
using Enset.Domain.Energy;
using Enset.Domain.GoldProfiles;

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
    Task<CanonicalSnapshotSet> GetPortfolio(
        CancellationToken cancellationToken);
}
