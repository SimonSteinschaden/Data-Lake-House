using Enset.Application.Imports.Enums;

namespace Enset.Application.Imports.DTOs.Api;

public sealed class CommitImportRequest
{
    public string UserId { get; init; } = string.Empty;

    public ImportTargetMode TargetMode { get; init; } = ImportTargetMode.Upsert;

    public ImportWriterType TargetWriter { get; init; }

    public string? TargetLocation { get; init; }

    public bool ArchiveRawSource { get; init; } = true;
}
