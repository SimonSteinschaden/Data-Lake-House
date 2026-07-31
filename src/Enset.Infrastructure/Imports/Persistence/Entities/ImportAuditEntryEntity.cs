namespace Enset.Infrastructure.Imports.Persistence.Entities;

public sealed class ImportAuditEntryEntity
{
    public Guid AuditId { get; set; }

    public Guid ImportId { get; set; }

    public ImportReportEntity ImportReport { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public Guid? IssueId { get; set; }

    public string? PreviousResolutionAction { get; set; }

    public string? ResolutionAction { get; set; }

    public string? PreviousCustomResolvedValue { get; set; }

    public string? CustomResolvedValue { get; set; }

    public string? Details { get; set; }
}
