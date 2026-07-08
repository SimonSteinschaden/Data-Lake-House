namespace Enset.Infrastructure.Imports.Persistence.Entities;

public sealed class ImportReportEntity
{
    public Guid ImportId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? SourceFileName { get; set; }

    public string? SourceFileContentType { get; set; }

    public long? SourceFileSizeBytes { get; set; }

    public string? SourceFileSha256 { get; set; }

    public string? SourceFileStagedPath { get; set; }

    public string? SourceFileRawStoragePath { get; set; }

    public int CustomerCount { get; set; }

    public int BuildingCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<ImportIssueEntity> Issues { get; set; } = new List<ImportIssueEntity>();

    public ICollection<ImportAuditEntryEntity> AuditTrail { get; set; } = new List<ImportAuditEntryEntity>();
}
