namespace Enset.Application.Associations;

public enum AssociationEntityType { Customer, Building, Meter, EnergySystem, MeterSeries, Document, Project, ImportBatch }
public enum AssociationConflictSeverity { Information, Warning, Blocking }

public sealed record AssociationTypeDefinition(
    string Key, AssociationEntityType SourceEntityType,
    AssociationEntityType TargetEntityType, string SourceLabel, string TargetLabel,
    IReadOnlyList<string> AllowedRoles, bool SupportsMultipleSources,
    bool SupportsMultipleTargets, bool SupportsPrimary, bool SupportsRole,
    bool SupportsValidity, bool SupportsHistory, string SourceCardinality,
    string TargetCardinality, string DeleteBehavior,
    IReadOnlyList<string> ConflictRules);

public sealed record AssociationEntityQuery(
    AssociationEntityType EntityType, string? Search = null, int Page = 1,
    int PageSize = 25, bool? IsActive = null, Guid? RelatedEntityId = null,
    string? Type = null, string? Status = null, string? City = null);
public sealed record AssociationEntityListItem(
    Guid Id, string DisplayName, string? SecondaryLabel, string Status,
    AssociationEntityType EntityType, IReadOnlyDictionary<string, string?> KeyFacts,
    int CurrentAssignmentsCount, string? QualityLevel);
public sealed record AssociationEntityPage(
    IReadOnlyList<AssociationEntityListItem> Items, int Page, int PageSize,
    int TotalCount, int TotalPages);

public sealed record AssociationPreviewRequest(
    string AssociationType, IReadOnlyList<Guid> SourceIds,
    IReadOnlyList<Guid> TargetIds, string? Role, DateOnly? ValidFrom,
    DateOnly? ValidTo, bool IsPrimary, bool ConfirmWarnings = false,
    string? Reason = null);
public sealed record ProposedAssociation(Guid SourceId, Guid TargetId,
    string? Role, DateOnly? ValidFrom, DateOnly? ValidTo, bool IsPrimary,
    string Status);
public sealed record AssociationConflict(
    string Code, AssociationConflictSeverity Severity, string Message,
    Guid? SourceId = null, Guid? TargetId = null);
public sealed record AssociationPreviewResponse(
    IReadOnlyList<ProposedAssociation> ProposedAssignments,
    IReadOnlyList<ProposedAssociation> ExistingAssignments,
    IReadOnlyList<AssociationConflict> Conflicts,
    IReadOnlyList<string> Warnings, IReadOnlyList<Guid> AffectedEntities,
    IReadOnlyList<string> Impacts, int TotalChanges, bool CanCommit);
public sealed record AssociationCommandResponse(
    Guid OperationId, int Created, int Updated, int Ended, int Skipped,
    IReadOnlyList<string> Warnings);
public sealed record AssociationListItem(
    Guid Id, string AssociationType, Guid SourceId, Guid TargetId,
    string? SourceDisplayName, string? TargetDisplayName, string? Role,
    DateOnly? ValidFrom, DateOnly? ValidTo, bool IsPrimary, bool IsActive);
public sealed record RemoveAssociationRequest(
    string AssociationType, IReadOnlyList<Guid> AssociationIds,
    DateOnly? EndDate, string? Reason, bool ConfirmWarnings = false);
public sealed record SetPrimaryAssociationRequest(
    string AssociationType, Guid AssociationId, string? Reason,
    bool ConfirmWarnings = false);
public sealed record AssociationAuditItem(
    Guid Id, Guid OperationId, DateTime ChangedAtUtc, Guid ChangedByUserId,
    string AssociationType, Guid SourceId, Guid TargetId, string Action,
    string? Before, string? After, string? Reason);

public interface IAssociationService
{
    IReadOnlyList<AssociationTypeDefinition> Types();
    Task<AssociationEntityPage> SearchEntities(AssociationEntityQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssociationListItem>> List(string associationType, Guid? sourceId,
        Guid? targetId, DateOnly? validAt, bool includeHistorical, CancellationToken cancellationToken);
    Task<AssociationPreviewResponse> Preview(AssociationPreviewRequest request, CancellationToken cancellationToken);
    Task<AssociationCommandResponse> Commit(AssociationPreviewRequest request, Guid userId, CancellationToken cancellationToken);
    Task<AssociationPreviewResponse> RemovePreview(RemoveAssociationRequest request, CancellationToken cancellationToken);
    Task<AssociationCommandResponse> Remove(RemoveAssociationRequest request, Guid userId, CancellationToken cancellationToken);
    Task<AssociationCommandResponse> SetPrimary(SetPrimaryAssociationRequest request, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssociationAuditItem>> History(string? associationType, Guid? sourceId,
        Guid? targetId, CancellationToken cancellationToken);
}
