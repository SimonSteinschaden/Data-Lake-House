namespace Enset.Application.Imports.Leb.DTOs;

public sealed class LebRowDto
{
    public int RowNumber { get; init; }
    /// <summary>
    /// All physical source cells keyed by their effective header. This includes
    /// generated and otherwise unmapped columns.
    /// </summary>
    public IReadOnlyDictionary<string, string?> SourceValues { get; init; }
        = new Dictionary<string, string?>();
    public string? MunicipalityId { get; init; }
    public string? MunicipalityName { get; init; }
    public string? BuildingId { get; init; }
    public string? BuildingName { get; init; }
    public string? ConstructionYear { get; init; }
    public string? FloorArea { get; init; }
    public string? Year { get; init; }
    public string? MeterId { get; init; }
    public string? MeterName { get; init; }
    public string? Type { get; init; }
    public string? Unit { get; init; }
    public string? SourceMedium { get; init; }
    public string? MeterGroup { get; init; }
    public IReadOnlyList<string?> MonthlyValues { get; init; } = [];
    public string? AnnualValue { get; init; }
}
