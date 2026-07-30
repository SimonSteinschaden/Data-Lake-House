namespace Enset.Infrastructure.Imports.MassImport;

public sealed class MeterReadingImportJobEntity
{
    public Guid JobId { get; set; }
    public Guid ImportId { get; set; }
    public string TargetMode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long ProcessedBytes { get; set; }
    public long ReadRows { get; set; }
    public long StagedRows { get; set; }
    public long ValidRows { get; set; }
    public long RejectedRows { get; set; }
    public long DuplicateRows { get; set; }
    public long WrittenRows { get; set; }
    public int CurrentBatch { get; set; }
    public Guid? UserId { get; set; }
    public DateTime AcceptedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool CancellationRequested { get; set; }
}

public sealed class ImportStagingMeterReading
{
    public long Id { get; set; }
    public Guid ImportId { get; set; }
    public Guid JobId { get; set; }
    public int BatchNumber { get; set; }
    public long SourceRowNumber { get; set; }
    public Guid? MeterId { get; set; }
    public string? MeterNumberOriginal { get; set; }
    public DateTime? Timestamp { get; set; }
    public decimal? Value { get; set; }
    public string? Unit { get; set; }
    public string? QualityFlag { get; set; }
    public string? ReadingType { get; set; }
    public string? EnergyDirection { get; set; }
    public int? IntervalSeconds { get; set; }
    public string ValidationStatus { get; set; } = "Pending";
    public string? ValidationCode { get; set; }
    public string? ValidationMessage { get; set; }
    public string RawSourceHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class MeterReadingImportAuditEntity
{
    public Guid Id { get; set; }
    public Guid ImportId { get; set; }
    public Guid JobId { get; set; }
    public Guid? MeterId { get; set; }
    public DateTime? StartTimestamp { get; set; }
    public DateTime? EndTimestamp { get; set; }
    public long ReadCount { get; set; }
    public long WrittenCount { get; set; }
    public long RejectedCount { get; set; }
    public long DuplicateCount { get; set; }
    public string Source { get; set; } = "Csv";
    public Guid? UserId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}
