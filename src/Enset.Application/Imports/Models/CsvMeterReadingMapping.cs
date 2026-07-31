using Enset.Application.Imports.Enums;

namespace Enset.Application.Imports.Models;

public sealed class CsvMeterReadingMapping
{
    public IReadOnlyList<string> DetectedHeaders { get; set; } = [];
    public IReadOnlyList<CsvRawRow> RawRows { get; set; } = [];
    public string? TimestampColumn { get; set; }
    public string? ValueColumn { get; set; }
    public string? QualityColumn { get; set; }
    public string? MeterNumberColumn { get; set; }
    public string? UnitColumn { get; set; }
    public ImportFieldSource TimestampSource { get; set; }
    public ImportFieldSource ValueSource { get; set; }
    public ImportFieldSource QualitySource { get; set; }
    public DateTime? StartTimestamp { get; set; }
    public TimeSpan? SamplingInterval { get; set; }
}

public sealed class CsvRawRow
{
    public int RowNumber { get; set; }
    public IReadOnlyDictionary<string, string?> Values { get; set; } =
        new Dictionary<string, string?>();
    public string? ParsingError { get; set; }
}
