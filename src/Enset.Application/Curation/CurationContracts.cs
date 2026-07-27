using Enset.Domain.Curation;

namespace Enset.Application.Curation;

public sealed record CurationTaskSummary(Guid Id, string EntityType, Guid EntityId,
    string EntityDisplayName, string FieldName, string? OriginalValue,
    string SuggestedValue, int ConfidencePercent, string Reasoning,
    CurationTaskStatus Status, string? CuratedValue, CurationSource Source);

public sealed record CurationDecisionDto(Guid Id, Guid UserId, DateTime DecidedAtUtc,
    CurationTaskStatus Decision, string? OriginalValue, string SuggestedValue,
    string? NewValue, CurationSource Source, int ConfidencePercent, string? Reason);

public sealed record CurationTaskDetail(CurationTaskSummary Task,
    IReadOnlyList<CurationDecisionDto> Decisions);

public sealed record CurationStatistics(int Bronze, int Silver, int Gold,
    int OpenTasks, IReadOnlyList<CurationTaskGroup> TaskGroups);

public sealed record CurationTaskGroup(string EntityType, string FieldName, int Count);
public sealed record CustomizeCurationRequest(string Value, string? Reason);
public sealed record RejectCurationRequest(string? Reason);

public interface ICurationService
{
    Task<IReadOnlyList<CurationTaskSummary>> GetTasksAsync(CancellationToken ct);
    Task<CurationTaskDetail?> GetTaskAsync(Guid id, CancellationToken ct);
    Task<CurationTaskDetail> AcceptAsync(Guid id, CancellationToken ct);
    Task<CurationTaskDetail> RejectAsync(Guid id, string? reason, CancellationToken ct);
    Task<CurationTaskDetail> CustomizeAsync(Guid id, string value, string? reason, CancellationToken ct);
    Task<CurationStatistics> GetStatisticsAsync(CancellationToken ct);
}

public sealed class CurationNotFoundException(string message) : Exception(message);
public sealed class CurationConflictException(string message) : Exception(message);
public sealed class CurationValidationException(string message) : Exception(message);
