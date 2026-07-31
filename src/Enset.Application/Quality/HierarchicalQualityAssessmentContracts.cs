using Enset.Domain.Curation;
using Enset.Domain.Quality;

namespace Enset.Application.Quality;

public sealed record MeterQualityAssessment(
    Guid MeterId, DataMaturityLevel QualityLevel,
    ProfileAnalysisStatus ProfileAnalysisStatus, Guid? CurrentAnalysisId,
    string? AnalysisVersion, decimal? CompletenessPercentage,
    int OpenIssueCount, int BlockingIssueCount, DateTime? LastAnalyzedAtUtc,
    string ConfirmationStatus, IReadOnlyList<string> BlockingReasons,
    IReadOnlyList<string> Warnings, IReadOnlyList<string> NextActions)
{
    public int OpenAnomalyCount { get; init; }
}

public sealed record EnergySystemQualityAssessment(
    Guid EnergySystemId, DataMaturityLevel QualityLevel,
    string ConfirmationStatus, IReadOnlyList<string> MissingRequirements,
    IReadOnlyList<string> BlockingReasons, IReadOnlyList<string> Warnings,
    IReadOnlyList<string> NextActions);

public sealed record OperationalBuildingQualityAssessment(
    Guid BuildingId, BuildingQualityAssessment Assessment,
    string InventoryDeclarationStatus,
    IReadOnlyList<string> NextActions)
{
    public IReadOnlyList<MeterQualityAssessment> MeterAssessments { get; init; } = [];
    public IReadOnlyList<EnergySystemQualityAssessment> EnergySystemAssessments { get; init; } = [];
    public bool NoRelevantEnergySystemsConfirmed { get; init; }
}

public interface IHierarchicalQualityAssessmentService
{
    Task<IReadOnlyDictionary<Guid, OperationalBuildingQualityAssessment>>
        AssessBuildings(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, MeterQualityAssessment>>
        AssessMeters(IReadOnlyCollection<Guid> ids, CancellationToken ct);
    Task<IReadOnlyDictionary<Guid, EnergySystemQualityAssessment>>
        AssessEnergySystems(IReadOnlyCollection<Guid> ids, CancellationToken ct);
}
