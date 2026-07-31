namespace Enset.Application.Imports.Reports;

public sealed class ImportSourceFileMetadata
{
    public string FileName { get; init; } = string.Empty;

    public string? ContentType { get; init; }

    public long Length { get; init; }

    public string Sha256 { get; init; } = string.Empty;

    public string StagedPath { get; init; } = string.Empty;

    public string? RawStoragePath { get; set; }
}
