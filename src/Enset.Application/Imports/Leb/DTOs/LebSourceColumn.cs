namespace Enset.Application.Imports.Leb.DTOs;

public sealed class LebSourceColumn
{
    /// <summary>One-based physical column position.</summary>
    public int Index { get; init; }
    public string? OriginalHeader { get; init; }
    public string EffectiveHeader { get; set; } = string.Empty;
    public bool WasHeaderGenerated { get; init; }
    public bool HasData { get; set; }
    public List<LebSourceColumnValue> Values { get; init; } = [];
}
