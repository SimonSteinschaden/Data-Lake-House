public class Meter : BaseEntity
{
    public Guid BuildingId { get; set; }
    public Building Building { get; set; } = null!;

    public MeterType Type { get; set; }

    public string Unit { get; set; } = "kWh";

    public string? ExternalId { get; set; }

    public ICollection<MeterReading> Readings { get; set; } = new List<MeterReading>();
}