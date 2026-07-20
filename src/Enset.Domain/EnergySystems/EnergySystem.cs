using Enset.Domain.Common;
using Enset.Domain.Geography;

namespace Enset.Domain.Energy;

public class EnergySystem : BaseEntity
{
    public string EnergySystemNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public EnergySystemType Type { get; set; }

    public string? ExternalIdentifier { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? AddressId { get; set; }

    public Address? Address { get; set; }

    public ICollection<EnergySystemBuildingAssignment> BuildingAssignments { get; set; }
        = new List<EnergySystemBuildingAssignment>();

    public ICollection<Meter> Meters { get; set; }
        = new List<Meter>();
}
