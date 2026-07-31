using Enset.Application.ReadModel;
using Enset.Domain.Quality;

namespace Enset.Application.Quality;

public sealed record InventoryDeclarationRequest(bool MeterInventoryComplete,
    bool EnergySystemInventoryComplete, bool NoRelevantEnergySystemsConfirmed,
    string? Comment, string? DisplayName);
public sealed record AnalysisCompletion(long ExpectedReadingCount, long ActualReadingCount,
    decimal CompletenessPercentage, long GapCount, long AnomalyCount,
    long BlockingIssueCount, long WarningCount, int? DetectedIntervalSeconds,
    string? DetectedUnit, string? Summary, ProfileAnalysisStatus Status);
public sealed record CurationDecisionRequest(ProfileDecisionType DecisionType,
    string? PreviousValue, string? NewValue, string? GeneratedValueMethod,
    int? ConfidencePercent, string Reason, string? Comment, bool IsFinal,
    Guid? SupersedesDecisionId, string? DisplayName);

public interface IQualityPersistenceService
{
    Task<BuildingInventoryDeclaration?> GetCurrentDeclaration(Guid buildingId, CancellationToken ct);
    Task<PagedResult<BuildingInventoryDeclaration>> GetDeclarationHistory(Guid buildingId, int page, int pageSize, CancellationToken ct);
    Task<BuildingInventoryDeclaration> DeclareInventory(Guid buildingId, InventoryDeclarationRequest request, CancellationToken ct);
    Task InvalidateInventory(Guid buildingId, string reason, CancellationToken ct);
    Task<MeterProfileAnalysis> StartAnalysis(Guid meterId, DateTime fromUtc, DateTime toUtc, string analysisVersion, CancellationToken ct);
    Task<MeterProfileAnalysis> CompleteAnalysis(Guid analysisId, AnalysisCompletion completion, CancellationToken ct);
    Task<MeterProfileAnalysis> FailAnalysis(Guid analysisId, bool cancelled, string summary, CancellationToken ct);
    Task<PagedResult<MeterProfileAnalysis>> GetAnalysisHistory(Guid meterId, int page, int pageSize, CancellationToken ct);
    Task<MeterProfileAnalysis?> GetAnalysis(Guid meterId, Guid analysisId, CancellationToken ct);
    Task AddIssues(Guid analysisId, IReadOnlyCollection<MeterProfileIssue> issues, CancellationToken ct);
    Task<PagedResult<MeterProfileIssue>> GetIssues(Guid analysisId, int page, int pageSize, CancellationToken ct);
    Task<MeterProfileCurationDecision> Decide(Guid issueId, CurationDecisionRequest request, CancellationToken ct);
    Task<PagedResult<MeterProfileCurationDecision>> GetDecisionHistory(Guid issueId, int page, int pageSize, CancellationToken ct);
}

public interface IQualityInvalidationService
{
    Task InvalidateBuildingInventory(Guid buildingId, string reason, CancellationToken ct);
    Task InvalidateMeterAnalysis(Guid meterId, string reason, CancellationToken ct);
    Task InvalidateBuildingConfirmations(Guid buildingId, string reason, CancellationToken ct);
}
