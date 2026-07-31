using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Reports;

public sealed class ImportAuditEntry
{
    public Guid AuditId { get; init; } = Guid.NewGuid();

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public string UserId { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public Guid? IssueId { get; init; }

    public ImportResolutionAction? PreviousResolutionAction { get; init; }

    public ImportResolutionAction? ResolutionAction { get; init; }

    public string? PreviousCustomResolvedValue { get; init; }

    public string? CustomResolvedValue { get; init; }

    public string? Details { get; init; }
}
