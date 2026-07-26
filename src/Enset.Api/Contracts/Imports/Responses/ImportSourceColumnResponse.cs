namespace Enset.Api.Contracts.Imports.Responses;

public sealed class ImportSourceColumnResponse
{
    public int Index { get; init; }
    public string? OriginalHeader { get; init; }
    public string EffectiveHeader { get; init; } = string.Empty;
    public bool WasHeaderGenerated { get; init; }
    public bool HasData { get; init; }
    public int ValueCount { get; init; }
}
