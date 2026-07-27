using Enset.Domain.Curation;

namespace Enset.Application.Curation;

public sealed record CurationTaskSummary(Guid Id, string EntityType, Guid EntityId,
    string EntityDisplayName, string FieldName, string? OriginalValue,
    string SuggestedValue, int ConfidencePercent, string Reasoning,
    CurationTaskStatus Status, string? CuratedValue, CurationSource Source,
    string RuleId, string RuleVersion, DataMaturityLevel MaturityLevel);

public sealed record CurationDecisionDto(Guid Id, Guid UserId, DateTime DecidedAtUtc,
    CurationTaskStatus Decision, string? OriginalValue, string SuggestedValue,
    string? NewValue, CurationSource Source, int ConfidencePercent, string? Reason);

public sealed record CurationTaskDetail(CurationTaskSummary Task,
    IReadOnlyList<CurationDecisionDto> Decisions);

public sealed record CurationStatistics(int Bronze, int Silver, int Gold,
    int OpenTasks, int RejectedTasks, int BuildingsWithoutUsageType,
    int BuildingsWithoutHeatedArea, int BuildingsWithoutPostalCode,
    int MeteringPointsWithoutUsageType, int MeteringPointsWithoutEnergyCarrier,
    int MeteringPointsWithIncompleteProfiles, IReadOnlyList<CurationTaskGroup> TaskGroups);

public sealed record CurationTaskGroup(string EntityType, string FieldName, int Count);
public sealed record CustomizeCurationRequest(string Value, string? Reason);
public sealed record RejectCurationRequest(string? Reason);
public sealed record CurationTaskQuery(int Page = 1, int PageSize = 25,
    string? EntityType = null, string? FieldName = null,
    CurationTaskStatus? Status = null, DataMaturityLevel? MaturityLevel = null,
    int? MinimumConfidence = null, Guid? CustomerId = null, Guid? BuildingId = null,
    Guid? MeteringPointId = null);
public sealed record CurationTaskPage(IReadOnlyList<CurationTaskSummary> Items,
    int Page, int PageSize, int TotalCount, int TotalPages);
public sealed record FieldReadiness(string FieldName, DataMaturityLevel MaturityLevel,
    bool Required, bool Satisfied, string Explanation, string? Value, CurationSource? Source);
public sealed record CurationReadiness(Guid EntityId, DataMaturityLevel MaturityLevel,
    int ReadinessPercent, bool IsGoldReady, IReadOnlyList<FieldReadiness> Fields,
    IReadOnlyList<string> BlockingIssues);
public sealed record BuildingGoldProfile(Guid BuildingId, Guid? CustomerId,
    string? UsageType, decimal? HeatedAreaSquareMeters, string? PostalCode,
    decimal? ElectricityConsumptionKwh, decimal? ProductionKwh,
    decimal? HwbKwhPerSquareMeterYear, BenchmarkState BenchmarkState,
    string? RenovationYear, string? BuildingType, string? Address,
    string? ConstructionYear, string? EnergyCarrier, string? ClimateRegion,
    string? AdditionalClassification, DataMaturityLevel MaturityLevel,
    int DataCompleteness, string QualitySummary,
    IReadOnlyList<FieldReadiness> FieldMaturity);
public sealed record MeteringPointGoldProfile(Guid MeteringPointId, Guid? BuildingId,
    Guid? CustomerId, string? UsageType, string? EnergyCarrier,
    string MeasurementType, string Unit, DateTime? MeasurementPeriodStart,
    DateTime? MeasurementPeriodEnd, int? IntervalMinutes, long ExpectedValueCount,
    long ActualValueCount, long MissingValueCount, long InvalidValueCount,
    long EstimatedValueCount, long InterpolatedValueCount,
    decimal CompletenessPercentage, decimal MeasuredPercentage,
    decimal DerivedPercentage, string? PostalCode, BenchmarkState BenchmarkState,
    DataMaturityLevel MaturityLevel, string QualitySummary,
    IReadOnlyList<FieldReadiness> FieldMaturity);

public interface ICurationService
{
    Task<CurationTaskPage> GetTasksAsync(CurationTaskQuery query, CancellationToken ct);
    Task<CurationTaskDetail?> GetTaskAsync(Guid id, CancellationToken ct);
    Task<CurationTaskDetail> AcceptAsync(Guid id, CancellationToken ct);
    Task<CurationTaskDetail> RejectAsync(Guid id, string? reason, CancellationToken ct);
    Task<CurationTaskDetail> CustomizeAsync(Guid id, string value, string? reason, CancellationToken ct);
    Task<CurationStatistics> GetStatisticsAsync(CancellationToken ct);
    Task<BuildingGoldProfile?> GetBuildingProfileAsync(Guid id, CancellationToken ct);
    Task<MeteringPointGoldProfile?> GetMeteringPointProfileAsync(Guid id, CancellationToken ct);
    Task<CurationReadiness?> GetBuildingReadinessAsync(Guid id, CancellationToken ct);
    Task<CurationReadiness?> GetMeteringPointReadinessAsync(Guid id, CancellationToken ct);
    Task<int> EvaluateBuildingAsync(Guid id, CancellationToken ct);
    Task<int> EvaluateMeteringPointAsync(Guid id, CancellationToken ct);
}

public sealed class CurationNotFoundException(string message) : Exception(message);
public sealed class CurationConflictException(string message) : Exception(message);
public sealed class CurationValidationException(string message) : Exception(message);
