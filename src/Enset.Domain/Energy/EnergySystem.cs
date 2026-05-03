public class EnergySystem : BaseEntity
{
    public Guid BuildingId { get; set; }
    public Building Building { get; set; } = null!;

    public EnergySystemType Type { get; set; }

    public decimal? Capacity { get; set; }

    public int? InstallationYear { get; set; }
}