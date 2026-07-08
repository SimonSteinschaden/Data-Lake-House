using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.DTOs.Api;

public sealed class ImportAuditEntryResponseDto
{
    public Guid AuditId { get; init; }

    public DateTime Timestamp { get; init; }

    public string UserId { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public Guid? IssueId { get; init; }

    public ImportResolutionAction? PreviousResolutionAction { get; init; }

    public ImportResolutionAction? ResolutionAction { get; init; }

    public string? PreviousCustomResolvedValue { get; init; }

    public string? CustomResolvedValue { get; init; }

    public string? Details { get; init; }
}
