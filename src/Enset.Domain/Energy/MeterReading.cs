public class MeterReading
{
    public Guid MeterId { get; set; }
    public Meter Meter { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public decimal Value { get; set; }

    public string Unit { get; set; } = "kWh";

    public DataQuality QualityFlag { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? BuildingId { get; set; }

    public Guid? SourceImportJobId { get; set; }
}