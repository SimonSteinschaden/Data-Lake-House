public class MeterReading : BaseEntity
{
    public Guid MeterId { get; set; }
    public Meter Meter { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public decimal Value { get; set; }

    public string Unit { get; set; } = "kWh";

    public DataQuality QualityFlag { get; set; }
}