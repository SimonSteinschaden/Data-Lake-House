using Enset.Domain.Data;

namespace Enset.Domain.Energy;

public class MeterReading : Enset.Domain.Common.BaseEntity
{
    public Guid MeterId { get; set; }

    public Meter Meter { get; set; } = null!;

    /// <summary>
    /// Zeitpunkt des Messwerts in UTC.
    /// </summary>
    public DateTime Timestamp { get; set; }

    public decimal Value { get; set; }

    public MeterReadingType ReadingType { get; set; }

    public DataQuality QualityFlag { get; set; }

    public int? IntervalSeconds { get; set; }

    public Guid? SourceImportJobId { get; set; }

    public Guid? SourceRawReadingId { get; set; }

    public ImportedMeterReading? SourceRawReading { get; set; }
}
