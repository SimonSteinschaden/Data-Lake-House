using Enset.Domain.Curation;using Enset.Domain.GoldProfiles;
namespace Enset.Application.GoldProfiles;
public sealed record GoldProfileVersionDto(Guid Id,string EntityType,Guid EntityId,int VersionNumber,string ProfileType,DateTime ValidFromUtc,DateTime? ValidToUtc,DateTime CreatedAtUtc,Guid CreatedByUserId,long SourceCurationRevision,string SnapshotHash,bool IsCurrent,GoldProfileReleaseStatus ReleaseStatus,string? ReleaseReason,uint RowVersion,string SnapshotJson)
{
    public string CreatedByDisplayName { get; init; } = "Unbekannt";
}
public interface IGoldProfileVersionService{
 Task<IReadOnlyList<GoldProfileVersionDto>> GetVersions(string type,Guid id,CancellationToken ct);Task<GoldProfileVersionDto?> Get(string type,Guid id,Guid versionId,CancellationToken ct);Task<GoldProfileVersionDto?> GetCurrent(string type,Guid id,CancellationToken ct);Task<GoldProfileVersionDto> Create(string type,Guid id,CancellationToken ct);Task<GoldProfileVersionDto> Release(string type,Guid id,Guid versionId,uint rowVersion,string? reason,CancellationToken ct);Task<GoldProfileVersionDto> Revoke(string type,Guid id,Guid versionId,uint rowVersion,string reason,CancellationToken ct);
}
public enum DataProductType{BuildingBenchmark,EnergyBenchmark,NormalizedLoadProfile,NormalizedGenerationProfile,EegMatching,PeerToPeerAnalysis}
public enum DataProductReadinessStatus{NotReady,PartiallyReady,ReadyWithWarnings,Ready}
public sealed record RequirementResult(string RequirementId,string Name,string Description,int Weight,bool IsBlocking,DataMaturityLevel MinimumMaturity,bool Fulfilled,string Guidance);
public sealed record ProfileVersionReference(string ProfileType,int Version,Guid VersionId);
public sealed record DataProductReadinessResult(DataProductType DataProductType,string ScopeType,Guid ScopeId,int Percentage,DataProductReadinessStatus Status,DataMaturityLevel MinimumMaturity,DateTime EvaluatedAtUtc,IReadOnlyList<RequirementResult> FulfilledRequirements,IReadOnlyList<RequirementResult> MissingRequirements,IReadOnlyList<RequirementResult> BlockingRequirements,IReadOnlyList<string> Warnings,IReadOnlyList<ProfileVersionReference> ProfileVersions,string DataCoverage);
public interface IDataProductReadinessService{Task<DataProductReadinessResult> Evaluate(DataProductType type,string scopeType,Guid scopeId,CancellationToken ct);Task<IReadOnlyList<DataProductReadinessResult>> EvaluateAll(string scopeType,Guid scopeId,CancellationToken ct);}
