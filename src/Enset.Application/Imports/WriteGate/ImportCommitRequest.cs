using Enset.Application.Imports.Enums;

namespace Enset.Application.Imports.WriteGate;

public sealed class ImportCommitRequest
{
    public Guid ImportId { get; init; }

    public string UserId { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public ImportTargetMode TargetMode { get; init; } = ImportTargetMode.Upsert;

    public ImportWriterType TargetWriter { get; init; }

    public string? TargetLocation { get; init; }

    public bool ArchiveRawSource { get; init; } = true;
}
