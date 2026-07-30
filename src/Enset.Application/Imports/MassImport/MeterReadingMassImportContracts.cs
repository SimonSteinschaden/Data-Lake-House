using Enset.Application.Imports.Enums;

namespace Enset.Application.Imports.MassImport;

public enum MeterReadingImportJobStatus
{
    Queued,
    Reading,
    Staging,
    Validating,
    Writing,
    Completed,
    Failed,
    Cancelled,
    Interrupted
}

public sealed record StartMeterReadingImportJob(
    Guid ImportId,
    ImportTargetMode TargetMode,
    Guid? UserId,
    long TotalBytes);

public sealed record MeterReadingImportJobProgress(
    Guid ImportId,
    Guid JobId,
    MeterReadingImportJobStatus Status,
    string Phase,
    long TotalBytes,
    long ProcessedBytes,
    long ReadRows,
    long StagedRows,
    long ValidRows,
    long RejectedRows,
    long DuplicateRows,
    long WrittenRows,
    int CurrentBatch,
    DateTime? StartedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    string? ErrorCode,
    string? ErrorMessage)
{
    public decimal ProgressPercent =>
        TotalBytes <= 0
            ? 0
            : Math.Min(
                100,
                decimal.Round(
                    ProcessedBytes * 100m / TotalBytes,
                    2));
}

public sealed record MeterReadingImportAccepted(
    Guid ImportId,
    Guid JobId,
    MeterReadingImportJobStatus Status,
    DateTime AcceptedAtUtc,
    string StatusUrl);

public sealed record MeterReadingImportCancelled(
    Guid ImportId,
    Guid JobId,
    MeterReadingImportJobStatus PreviousStatus,
    MeterReadingImportJobStatus CurrentStatus);

public interface IMeterReadingMassImportJobs
{
    Task<MeterReadingImportAccepted> Enqueue(
        StartMeterReadingImportJob command,
        CancellationToken cancellationToken);
    Task<MeterReadingImportJobProgress?> Get(
        Guid importId,
        CancellationToken cancellationToken);
    Task<MeterReadingImportCancelled?> Cancel(
        Guid importId,
        CancellationToken cancellationToken);
}
