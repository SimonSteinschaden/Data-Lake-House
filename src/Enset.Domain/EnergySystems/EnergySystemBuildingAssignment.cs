using Enset.Domain.Buildings;
using Enset.Domain.Common;

namespace Enset.Domain.Energy;

public class EnergySystemBuildingAssignment : BaseEntity
{
    public Guid EnergySystemId { get; set; }

    public EnergySystem EnergySystem { get; set; } = null!;

    public Guid BuildingId { get; set; }

    public Building Building { get; set; } = null!;

    public EnergySystemBuildingRole Role { get; set; }

    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

    public DateTime? ValidTo { get; set; }
}