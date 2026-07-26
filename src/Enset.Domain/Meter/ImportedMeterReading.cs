using Enset.Domain.Common;

namespace Enset.Domain.Energy;

/// <summary>
/// Unveränderte und zusätzlich geparste Sicht einer importierten Messwertzeile.
/// Sie ist die persistente Grundlage für spätere Validierung und Vergoldung.
/// </summary>
public sealed class ImportedMeterReading : BaseEntity
{
    public Guid ImportId { get; set; }

    public Guid? MeterId { get; set; }

    public Meter? Meter { get; set; }

    public string? MeterNumberRaw { get; set; }

    public string? TimestampRaw { get; set; }

    public string? ValueRaw { get; set; }

    public string? QualityRaw { get; set; }

    public DateTime? Timestamp { get; set; }

    public decimal? Value { get; set; }

    public int? Quality { get; set; }

    public int? RowNumber { get; set; }

    public string? SourceName { get; set; }

    public string? ParsingError { get; set; }

    public ICollection<MeterReading> CuratedReadings { get; set; } =
        new List<MeterReading>();
}
