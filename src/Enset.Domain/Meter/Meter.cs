using Enset.Domain.Buildings;
using Enset.Domain.Common;
using Enset.Domain.EnergyCommunities;

namespace Enset.Domain.Energy;

public class Meter : BaseEntity
{
    public string MeterNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ExternalIdentifier { get; set; }

    public MeterMedium Medium { get; set; }

    public MeterQuantity Quantity { get; set; }

    public MeterUnit Unit { get; set; }

    public MeterDirection Direction { get; set; }

    public MeterType Type { get; set; }

    public string? Manufacturer { get; set; }

    public string? Model { get; set; }

    public string? SerialNumber { get; set; }

    public string? CommunicationProtocol { get; set; }

    public Guid? BuildingId { get; set; }

    public Building? Building { get; set; }

    public Guid? EnergySystemId { get; set; }

    public EnergySystem? EnergySystem { get; set; }

    public DateTime? CommissionedAt { get; set; }

    public DateTime? DecommissionedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<MeterReading> Readings { get; set; }
        = new List<MeterReading>();
    
    public ICollection<EnergyCommunityMeterAssignment>
        EnergyCommunityAssignments { get; set; }
        = new List<EnergyCommunityMeterAssignment>();
}