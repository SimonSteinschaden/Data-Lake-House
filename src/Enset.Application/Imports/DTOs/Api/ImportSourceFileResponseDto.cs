namespace Enset.Application.Imports.DTOs.Api;

public sealed class ImportSourceFileResponseDto
{
    public string FileName { get; init; } = string.Empty;

    public string? ContentType { get; init; }

    public long Length { get; init; }

    public string Sha256 { get; init; } = string.Empty;

    public bool IsRawArchived { get; init; }
}
